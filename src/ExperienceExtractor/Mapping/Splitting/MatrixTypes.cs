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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.Helpers;

namespace ExperienceExtractor.Mapping.Splitting
{

    public static class MatrixTypes
    {

        public static IEnumerable<Tuple<object, object>> CoOcurrence(IEnumerable<object> items)
        {
            var array = items.ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                for (var j = 0; j < array.Length; j++)
                {
                    yield return Tuple.Create(array[i], array[j]);
                }
            }
        }

        public static IEnumerable<Tuple<object, object>> Links(IEnumerable<object> items)
        {
            var array = items.ToArray();
            for (var i = 0; i < array.Length - 1; i++)
            {
                yield return Tuple.Create(array[i], array[i + 1]);
            }
        }
    }
}
