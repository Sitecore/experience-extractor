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
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ExperienceExtractor.Api.Jobs
{
    [DataContract]
    public class JobInfo
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public DateTime Created { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime? Ended { get; set; }

        [DataMember]
        public long ItemsProcessed { get; set; }
        [DataMember]
        public double? Progress { get; set; }

        [DataMember]
        [JsonConverter(typeof(StringEnumConverter))]
        public JobStatus Status { get; set; }

        [DataMember]
        public string StatusText { get; set; }

        [DataMember]
        public bool SizeLimitExceeded { get; set; }

        
        public bool HasResult { get; set; }

        [DataMember]
        public string Url { get; set; }
        [DataMember(EmitDefaultValue = false)]
        public string ResultUrl { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Specification { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string LastException { get; set; }
                
        public static JobInfo FromJob(Job job)
        {            
            return new JobInfo
            {
                Created = job.Created,
                Ended = job.EndDate,
                ItemsProcessed = job.ItemsProcessed,
                LastException = job.LastException != null ? job.LastException.ToString() : null,
                Progress = job.Progress,
                Status = job.Status,
                StatusText = job.StatusText,
                Specification = job.Specification != null ? job.Specification.ToString() : null,
                SizeLimitExceeded = job.SizeLimitExceeded,
                Id = job.Id
            };
        }
    }
}