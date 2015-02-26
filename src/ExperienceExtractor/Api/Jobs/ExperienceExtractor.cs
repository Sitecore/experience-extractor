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

using System.Collections.Generic;
using System.Web.Hosting;
using ExperienceExtractor.Api.Parsing;

namespace ExperienceExtractor.Api.Jobs
{
    public class ExperienceExtractorApiContainer
    {
        public static IJobRepository JobRepository { get; set; }

        public static ParseFactories ParseFactories { get; set; }

        public static Dictionary<string, string> ItemPaths { get; set; }

        static ExperienceExtractorApiContainer()
        {            
            JobRepository = LocalJobRepository.Instance;
            ParseFactories = ExperienceExtractor.Api.Parsing.ParseFactories.Default;
            
            ItemPaths = new Dictionary<string, string>();
        }
    }
}
