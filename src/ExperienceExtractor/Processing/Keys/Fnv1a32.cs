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

namespace ExperienceExtractor.Processing.Keys
{
    public class Fnv1a32 : KeyFactoryBase<int>
    {
        public Type Type {get { return typeof (int); }}
        public override object CalculateKey(IEnumerable<object> values)
        {
            unchecked
            {
                const int fnv32Prime = (int) 16777619U;
                var hash = (int)2166136261U;
                foreach (var o in values)
                {
                    hash = (hash ^ (o == null ? 0 : o.GetHashCode()))*fnv32Prime;
                }

                return hash;
            }
        }
    }
}
