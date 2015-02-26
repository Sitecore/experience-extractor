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
using System.Linq;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Processing.Labels
{
    public class LabelLoader
    {        
        public ILabelProvider LabelProvider { get; private set; }

        private readonly LruCache<object, object> _cache;   

        public LabelLoader(ILabelProvider labelProvider, int cacheSize = 1000)
        {
            LabelProvider = labelProvider;            
            _cache = new LruCache<object, object>(cacheSize);
        }
             
        public void SetLabels(IEnumerable<IList<object>> rows, int keyIndex, int labelIndex)
        {                      
            foreach (var row in rows)
            {                
                var key = row[keyIndex];                
                if (key != null)
                {                    
                    if (row[labelIndex] == null)
                    {
                        row[labelIndex] = _cache.GetOrAdd(key, LabelProvider.GetLabel);                                              
                    }
                }
            }            
        }       
    }
}
