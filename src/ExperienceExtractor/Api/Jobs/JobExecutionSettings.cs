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
using System.Text;
using System.Threading.Tasks;

namespace ExperienceExtractor.Api.Jobs
{
    public class JobExecutionSettings
    {
        public string TempDirectory { get; set; }

        public int ProcessingThreads { get; set; }

        public int LoadThreads { get; set; }

        public int BatchSize { get; set; }

        public long? SizeLimit { get; set; }

        public int DataSourceBufferSize { get; set; }

        public int FieldCacheSize { get; set; }

        public string DatabaseName { get; set; }
    }
}
