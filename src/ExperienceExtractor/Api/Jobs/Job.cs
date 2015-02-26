//--------------------------------------------------------------------------------------------
// Copyright 2015 Sitecore Corporation A/S
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file 
// except in compliance with the License. You may obtain a copy of the License at
//       http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the 
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
// either express or implied. See the License for the specific language governing permissions 
// and limitations under the License.
// -------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using ExperienceExtractor.Mapping;
using Sitecore.Data;
using Sitecore.Diagnostics;
using ExperienceExtractor.Export;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Api.Jobs
{
    public class Job : IDisposable
    {
        public Guid Id { get; private set; }

        public IJobSpecification Specification { get; private set; }

        public string TempDirectory { get; set; }

        public int StatusUpdateFrequency { get; set; }

        public JobExecutionSettings ExecutionSettings { get; set; }

        /// <summary>
        /// The estimated number of items to process client side. This number if before client side filters are applied
        /// </summary>
        public long? EstimatedClientItemCount { get; private set; }

        public long ItemsProcessed { get; private set; }

        public double? Progress
        {
            get
            {
                return EstimatedClientItemCount.HasValue
                    ? 
                    EstimatedClientItemCount.Value == 0 ? 1 :
                        Math.Min(1d, ItemsProcessed / (double)EstimatedClientItemCount.Value)
                    : (double?)null;
            }
        }

        public DateTime Created { get; private set; }
        public DateTime? EndDate { get; private set; }


        public JobStatus Status { get; private set; }
        public string StatusText { get; private set; }

        public event EventHandler StatusChanged;
        public event EventHandler JobEnded;

        public bool SizeLimitExceeded { get; private set; }


        public ITableDataPostProcessor CurrentPostProcessor { get; private set; }

        public ITableDataPostProcessor[] PostProcessors { get; private set; }

        public Exception LastException { get; private set; }


        public Job(IJobSpecification specification, JobExecutionSettings settings, Guid? id = null, int statusUpdateFrequency = 1000)
        {
            Id = id ?? Guid.NewGuid();
            Created = DateTime.Now;

            Status = JobStatus.Pending;
            Specification = specification;

            StatusUpdateFrequency = statusUpdateFrequency;
            ExecutionSettings = settings;
            TempDirectory = Path.Combine(settings.TempDirectory, Id.ToString("N"));
        }


        public void Run()
        {
            if (Status != JobStatus.Pending) throw new InvalidOperationException("Job is not pending");

            try
            {
                Directory.CreateDirectory(TempDirectory);

                File.WriteAllText(Path.Combine(TempDirectory, "Specification.txt"), Specification.ToString());

                SetStatus(JobStatus.Preparing);

                Specification.Initialize(this);

                var source = Specification.CreateDataSource();
                EstimatedClientItemCount = source.Count;

                PostProcessors = Specification.CreatePostProcessors().ToArray();

                foreach (var proc in PostProcessors)
                {
                    proc.Validate();
                }

                var jobDirectory = TempDirectory;

                //The processors will consume data from this collection;
                var items = new BlockingCollection<object>(ExecutionSettings.DataSourceBufferSize);

                //Create the processors
                var processors = Enumerable.Range(0, ExecutionSettings.ProcessingThreads).Select(i =>
                {
                    IItemFieldLookup lookup = null;
                    try
                    {
                        lookup = new ItemDatabaseFieldLookup(Database.GetDatabase(ExecutionSettings.DatabaseName),
                            Specification.DefaultLanguage,
                            ExecutionSettings.FieldCacheSize);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error initailizing item field lookup", ex, this);
                    }

                    var proc = new DataProcessor(Specification.CreateRootMapper())
                    {
                        BatchSize = ExecutionSettings.BatchSize,                        
                        FieldLookup = lookup
                    };

                    proc.BatchWriter = new TableDataBatchWriter(new CsvExporter(jobDirectory)
                    {
                        PartitionPrefix = "~" + i + "_",
                    })
                    {
                        SyncLock = this,
                        MaximumSize = ExecutionSettings.SizeLimit
                    };

                    return proc;
                }).ToArray();


                
                //Start the processors
                var processingThreads = processors.Select(p =>
                {
                    var t = new Thread(() =>
                    {
                        try
                        {
                            p.Process(items.GetConsumingEnumerable());
                        }
                        catch (Exception ex)
                        {
                            LastException = ex;
                            SetStatus(JobStatus.Failed, ex.Message);
                        }
                    });
                    t.Start();
                    return t;
                }).ToArray();


                if (Status == JobStatus.Failed)
                {
                    throw LastException;
                }
                
                SetStatus(JobStatus.Running);


                source.ItemLoaded += (sender, args) =>
                {
                    if (StatusUpdateFrequency <= 0 || ItemsProcessed % StatusUpdateFrequency == 0)
                    {
                        OnProgress();
                    }
                    ItemsProcessed = args;
                };

                //Add items to the collection that the processors consume
                foreach (var item in source)
                {
                    if (Status != JobStatus.Running)
                    {
                        break;
                    }
                    
                    if (processors.Any(p => p.BatchWriter.End))
                    {
                        break;
                    }
                    items.Add(item);
                }
                items.CompleteAdding();

                //Wait for processors to finish
                foreach (var p in processingThreads)
                {
                    p.Join();
                }

                if (Status == JobStatus.Running)
                {
                    //Now we know how many items we got for sure. Update progress to 100%
                    EstimatedClientItemCount = ItemsProcessed;

                    SetStatus(JobStatus.Merging);

                    var csvWriter = Specification.CreateExporter(jobDirectory);

                    var tables = csvWriter.Export(MergedTableData.FromTableSets(processors.Select(p => p.Tables)))
                        .Cast<CsvTableData>().ToArray();

                    foreach (var proc in processors)
                    {
                        SizeLimitExceeded = SizeLimitExceeded || proc.BatchWriter.End;
                        proc.BatchWriter.Dispose();
                    }

                    foreach (var postProcessor in PostProcessors)
                    {
                        CurrentPostProcessor = postProcessor;
                        SetStatus(JobStatus.PostProcessing, postProcessor.Name);

                        postProcessor.Process(jobDirectory, tables);
                    }
                    CurrentPostProcessor = null;

                    SetStatus(JobStatus.Completing);

                    SetStatus(JobStatus.Completed);
                }

            }
            catch (Exception ex)
            {
                Log.Error("Job failed", ex);
                LastException = ex;
                SetStatus(JobStatus.Failed, ex.ToString());
            }

            EndDate = DateTime.Now;
            try
            {
                OnJobEnded();
            }
            catch (Exception ex)
            {
                Log.Error("Exception occured after job ended", ex, this);
                LastException = ex;
            }

            try
            {
                if (Status == JobStatus.Canceled || Status == JobStatus.Failed)
                {
                    Delete();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception occured while deleting job", ex, this);
            }
        }

        void SetStatus(JobStatus status, string text = null)
        {
            Status = status;
            StatusText = text;
            OnProgress();
        }

        protected virtual void OnProgress()
        {
            if (StatusChanged != null) StatusChanged(this, EventArgs.Empty);
        }

        protected virtual void OnJobEnded()
        {
            if (JobEnded != null) JobEnded(this, EventArgs.Empty);
        }

        void Delete()
        {
            if (!string.IsNullOrEmpty(TempDirectory) && Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory);
            }
        }


        public void Cancel()
        {
            if (Status <= JobStatus.Running)
            {
                Status = JobStatus.Canceled;
                OnProgress();
            }
            else
            {
                Delete();
            }
        }

        public void Dispose()
        {
            Cancel();
        }
    }
}
