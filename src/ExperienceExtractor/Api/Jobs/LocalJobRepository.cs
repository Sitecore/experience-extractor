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

        public int MaxJobHistoryLength { get; set; }



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

            MaxJobHistoryLength = ExperienceExtractorWebApiConfig.MaxJobHistoryLength;
        }

        private readonly Dictionary<string, List<Job>> _jobsByLockKey = new Dictionary<string, List<Job>>();
        private readonly object _jobListSyncRoot = new object();

        public JobInfo CreateJob(IJobSpecification specification, Action<JobInfo> jobEnded = null)
        {
            var job = Add(specification);

            Task.Run(() =>
            {
                try
                {
                    if (jobEnded != null)
                    {
                        job.JobEnded += (sender, args) => jobEnded(GetJobInfo((Job)sender));
                    }
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

        private Job Add(IJobSpecification specification)
        {
            Job job;
            lock (_jobListSyncRoot)
            {
                var lockKey = specification.LockKey;
                List<Job> jobList = null;
                if (!string.IsNullOrEmpty(lockKey))
                {
                    jobList = GetJobList(lockKey, true);
                    if (jobList.Any(j => !j.EndDate.HasValue))
                    {
                        throw new InvalidOperationException(string.Format("Another job is running with the lock key '{0}'", lockKey));
                    }
                }

                job = new Job(specification, JobExecutionSettings);
                if (!_jobs.TryAdd(job.Id, job))
                {
                    throw new InvalidOperationException("Dupplicate job ID");
                }
                if (jobList != null)
                {
                    jobList.Add(job);
                }
            }

            PurgeOld();

            return job;
        }


        private void PurgeOld()
        {
            if (_jobs.Count < MaxJobHistoryLength) return;

            var endedJobs = _jobs.Values.Where(job => job.EndDate.HasValue).ToArray();
            if (endedJobs.Length >= MaxJobHistoryLength)
            {
                var oldestJob = endedJobs.OrderBy(job => job.EndDate.Value).FirstOrDefault();
                if (oldestJob != null)
                {
                    Remove(oldestJob);
                }
            }
        }


        private void Remove(Job job)
        {
            Job removed;
            if (_jobs.TryRemove(job.Id, out removed)
                && !string.IsNullOrEmpty(removed.Specification.LockKey))
            {
                lock (_jobListSyncRoot)
                {
                    var list = GetJobList(removed.Specification.LockKey);
                    if (list != null)
                    {
                        list.Remove(job);
                    }
                }
            }
        }



        private List<Job> GetJobList(string lockKey, bool add = false)
        {
            lock (_jobListSyncRoot)
            {
                List<Job> jobs;
                if (!_jobsByLockKey.TryGetValue(lockKey, out jobs))
                {
                    if (add)
                    {
                        _jobsByLockKey.Add(lockKey, jobs = new List<Job>());
                    }
                    else
                    {
                        return null;
                    }
                }
                return jobs;
            }
        }

        public IEnumerable<JobInfo> GetFromLockKey(string lockKey)
        {
            lock (_jobListSyncRoot)
            {
                var jobs = GetJobList(lockKey, false);
                return jobs != null ? jobs.Select(GetJobInfo).ToArray() : Enumerable.Empty<JobInfo>();
            }
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
                        if (zipFile.Exists) zipFile.Delete();
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

            return info;
        }
    }
}