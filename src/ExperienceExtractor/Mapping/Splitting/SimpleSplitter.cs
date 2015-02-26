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
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping.Splitting
{
    public class SimpleSplitter : ISplitter
    {
        public string[] Names { get; private set; }
        public IEnumerable<object> GetSplits(ProcessingScope scope)
        {
            return Selector(scope.CurrentObject);
        }

        public Func<object, IEnumerable<object>> Selector { get; private set; }

        public SimpleSplitter(IEnumerable<string> names, Func<object, IEnumerable<object>> selector)
        {
            Names = names.ToArray();
            Selector = selector;
        }
    }    
}
