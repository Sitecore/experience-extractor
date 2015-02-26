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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Diagnostics;
using ExperienceExtractor.Api.Http.Configuration;
using ExperienceExtractor.Processing.Helpers;

namespace ExperienceExtractor.Api.Jobs
{
    public class LocalJobRepository : IJobRepository
    {
        private static readonly LocalJobRepository _instance = new LocalJobRepository();

        public static LocalJobRepository Instance
        {
            get { return _instance; }
        }


        public JobExecutionSettings JobExecutionSettings { get; set; }

        public string TargetUrl { get; private set; }
        private ConcurrentDictionary<Guid, Job> _jobs = new ConcurrentDictionary<Guid, Job>();
        

        public LocalJobRepository()
            : this(ExperienceExtractorWebApiConfig.JobExecutionSettings)
        {
            
        }

        public LocalJobRepository(JobExecutionSettings settings)
        {            
            JobExecutionSettings = settings;                     
        }

               
        public JobInfo CreateJob(IJobSpecification specification)
        {
            var job = new Job(specification, JobExecutionSettings);            

            _jobs.TryAdd(job.Id, job);
            

            Task.Run(() =>
            {
                try
                {                   
                    job.Run();
                }
                catch (Exception ex)
                {
                    Log.Error("Error running job", ex, this);
                }
            });

            return GetJobInfo(job);
        }

        public IEnumerable<JobInfo> Get()
        {
            return _jobs.Values.Select(GetJobInfo);
        }

        Job FindJob(Guid id)
        {
            return _jobs.GetOrDefault(id);            
        }

        public JobInfo Get(Guid id)
        {
            var job = FindJob(id);
            return job != null ? GetJobInfo(job) : null;
        }

        public JobInfo Cancel(Guid id)
        {
            var job = FindJob(id);
            if (job != null)
            {
                job.Cancel();
                var zipPath = GetZipPath(id);
                if (zipPath.Exists)
                {
                    zipPath.Delete();
                }
                return GetJobInfo(job);
            }

            return null;
        }


        private FileInfo GetZipPath(Guid id)
        {
            return new FileInfo(Path.Combine(JobExecutionSettings.TempDirectory, "zip", id.ToString("d") + ".zip"));
        }

        public Stream GetZippedResult(Guid id)
        {
            var zipFile = GetZipPath(id);

            if (!zipFile.Exists)
            {
                var job = FindJob(id);
                if (job != null)
                {
                    if (!zipFile.Directory.Exists) zipFile.Directory.Create();
                    try
                    {
                        ZipFile.CreateFromDirectory(job.TempDirectory, zipFile.FullName);
                    }
                    catch
                    {
                        zipFile.Refresh();
                        if( zipFile.Exists) zipFile.Delete();
                        throw;
                    }
                }
            }

            zipFile.Refresh();
            return zipFile.Exists ? File.OpenRead(zipFile.FullName) : null;
        }

        protected virtual JobInfo GetJobInfo(Job job)
        {
            var info = JobInfo.FromJob(job);

            if (info.Status == JobStatus.Completed)
            {
                info.HasResult = true;                
            }

            return info;
        }
    }
}