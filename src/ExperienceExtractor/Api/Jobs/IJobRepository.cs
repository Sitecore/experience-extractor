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
using System.IO;

namespace ExperienceExtractor.Api.Jobs
{
    public interface IJobRepository
    {
        JobInfo CreateJob(IJobSpecification specification, Action<JobInfo> jobEnded = null);

        IEnumerable<JobInfo> Get();

        IEnumerable<JobInfo> GetFromLockKey(string lockKey);

        JobInfo Get(Guid id);

        JobInfo Cancel(Guid id);

        Stream GetZippedResult(Guid id);
    }
}