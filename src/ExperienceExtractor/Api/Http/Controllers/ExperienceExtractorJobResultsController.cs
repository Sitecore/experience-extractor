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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;
using ExperienceExtractor.Api.Jobs;

namespace ExperienceExtractor.Api.Http.Controllers
{
    [RequireSitecoreLogin]
    public class ExperienceExtractorJobResultsController : ApiController
    {
        private readonly IJobRepository _repository;

        public ExperienceExtractorJobResultsController()
            : this(ExperienceExtractorApiContainer.JobRepository)
        {

        }

        public ExperienceExtractorJobResultsController(IJobRepository repository)
        {
            _repository = repository;
        }

        // GET api/jobs/{id}/result
        public HttpResponseMessage Get(Guid id)
        {
            var stream = _repository.GetZippedResult(id);
            if (stream == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var result = new HttpResponseMessage(HttpStatusCode.OK) {Content = new StreamContent(stream)};

           result.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/zip");

            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = id.ToString("d") + ".zip"
            };

            return result;
        }
    }
}
