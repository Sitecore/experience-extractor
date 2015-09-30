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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.ModelBinding;
using Newtonsoft.Json.Linq;
using ExperienceExtractor.Api.Http.Configuration;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Api.Parsing;
using Sitecore.Pipelines.Save;

namespace ExperienceExtractor.Api.Http.Controllers
{
    [RequireSitecoreLogin]
    public class ExperienceExtractorJobsController : ApiController
    {
        private readonly IJobRepository _repository;

        public ExperienceExtractorJobsController()
            : this(ExperienceExtractorApiContainer.JobRepository)
        {

        }

        public ExperienceExtractorJobsController(IJobRepository repository)
        {
            _repository = repository;
        }

        // GET api/jobs
        public IEnumerable<JobInfo> Get(string lockKey = null)
        {
            if (lockKey != null)
            {
                return _repository.GetFromLockKey(lockKey).Select(info => UpdateResultUrl(info, false));
            }
            return _repository.Get().Select(info=>UpdateResultUrl(info, false));
        }

        // GET api/jobs/(guid)
        public IHttpActionResult Get(Guid id)
        {
            var info = _repository.Get(id);
            return info == null ? NotFound() : (IHttpActionResult)Ok(UpdateResultUrl(info, true));
        }


        // POST api/jobs
        public async Task<IHttpActionResult> Post(HttpRequestMessage request)
        {
            var content = await request.Content.ReadAsStringAsync();
            try
            {
                var specification = new JsonJobParser(JObject.Parse(content));
                var jobInfo = _repository.CreateJob(specification);

                return RedirectToRoute(ExperienceExtractorWebApiConfig.JobRouteName, new { id = jobInfo.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }

        // DELETE api/jobs/(guid)
        public IHttpActionResult Delete(Guid id)
        {
            return _repository.Cancel(id) != null ? Ok() : (IHttpActionResult) NotFound();            
        }

        private JobInfo UpdateResultUrl(JobInfo jobInfo, bool includeDetails)
        {
            jobInfo.Url = Url.Route(ExperienceExtractorWebApiConfig.JobRouteName, new {id = jobInfo.Id});

            if (jobInfo.HasResult)
            {
                jobInfo.ResultUrl = Url.Route(ExperienceExtractorWebApiConfig.JobResultRouteName, new {id = jobInfo.Id});
            }

            if (!includeDetails)
            {
                jobInfo.Specification = null;
                jobInfo.LastException = null;
            }

            return jobInfo;
        }
    }
}
