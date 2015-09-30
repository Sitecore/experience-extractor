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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using Newtonsoft.Json;
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

        public long RowsCreated { get; private set; }

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

                File.WriteAllText(Path.Combine(TempDirectory, "specification.json"), Specification.ToString());

                SetStatus(JobStatus.Preparing);

                Specification.Initialize(this);

                var source = Specification.CreateDataSource();                

                PostProcessors = Specification.CreatePostProcessors().ToArray();

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

                    var exporter =  Specification.CreateExporter(jobDirectory) as CsvExporter; //Move PartititionPrefix so this cast isn't necessary
                    if (exporter == null)
                    {
                        exporter = new CsvExporter(jobDirectory);                        
                    }
                    exporter.PartitionPrefix = "~" + i + "_";
                    exporter.KeepOutput = true; //Don't delete the job's main directory
                    proc.BatchWriter = new TableDataBatchWriter(exporter)                    
                    {
                        SyncLock = this,
                        MaximumSize = ExecutionSettings.SizeLimit
                    };

                    proc.Initialize();

                    return proc;
                }).ToArray();



                var hasUpdates = false;                
                //Allow post processors to validate their conditions (if any). This allows the job to fail before data is processed
                //Allow post processors to filter data source for updates
                foreach (var pp in PostProcessors)
                {
                    pp.Validate(processors[0].Tables, Specification);
                    if (pp.UpdateDataSource(processors[0].Tables, source))
                    {
                        if (hasUpdates)
                        {
                            throw new InvalidOperationException("Only one post processor can update the data source");
                        }
                        hasUpdates = true;
                    }
                }

                EstimatedClientItemCount = source.Count;
                
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
                    RowsCreated = processors.Sum(p => p.RowsCreated);
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

                RowsCreated = processors.Sum(p => p.RowsCreated);

                if (Status == JobStatus.Running)
                {
                    //Now we know how many items we got for sure. Update progress to 100%
                    EstimatedClientItemCount = ItemsProcessed;

                    SetStatus(JobStatus.Merging);

                    using (var csvWriter = Specification.CreateExporter(TempDirectory))
                    {

                        var tables = MergedTableData.FromTableSets(processors.Select(p => p.Tables)).ToArray();

                        
                        var w = csvWriter as CsvExporter;
                        if (w == null || w.KeepOutput)
                        {                            
                            tables = csvWriter.Export(tables).ToArray();
                        }


                        File.WriteAllText(Path.Combine(jobDirectory, "schema.json"),
                            tables.Select(t => t.Schema).Serialize());


                        
                        foreach (var postProcessor in PostProcessors)
                        {
                            CurrentPostProcessor = postProcessor;
                            SetStatus(JobStatus.PostProcessing, postProcessor.Name);

                            postProcessor.Process(jobDirectory, tables, Specification);
                        }
                        CurrentPostProcessor = null;

                        foreach (var proc in processors)
                        {                            
                            SizeLimitExceeded = SizeLimitExceeded || proc.BatchWriter.End;
                            proc.BatchWriter.Dispose();
                        }

                        SetStatus(JobStatus.Completing);
                    }
                    SetStatus(JobStatus.Completed);
                }

            }
            catch (Exception ex)
            {
                Log.Error("Job failed", ex, this);
                LastException = ex;
                SetStatus(JobStatus.Failed, ex.ToString());
            }
            
            try
            {
                OnJobEnded();
            }
            catch (Exception ex)
            {
                Log.Error("Exception occured after job ended", ex, this);
                LastException = ex;
            }
            EndDate = DateTime.Now;

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
                Directory.Delete(TempDirectory, true);
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
