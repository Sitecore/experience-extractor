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
using System.Linq;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping.Splitting
{
    /// <summary>
    /// Creates nested tables from a splitter with a table for each split.    
    /// </summary>
    public class SplittingTableMapper : ITableMapper
    {        
        public ISplitter Splitter { get; set; }

        public ITableMapper[] TableMappers { get; private set; }

        public SplittingTableMapper(ISplitter splitter,                        
            Func<string, ITableMapper> factory)           
        {            
            Splitter = splitter;
            TableMappers = splitter.Names.Select(factory).ToArray();
        }
       
        
        public void Initialize(DataProcessor processor, TableDataBuilder parentTable)
        {
            foreach (var mapper in TableMappers)
            {
                mapper.Initialize(processor, parentTable);
            }
        }

        public void Process(ProcessingScope context)
        {
            var i = 0;
            foreach (var split in Splitter.GetSplits(context))
            {
                using (context.Swap(split))
                {
                    TableMappers[i++].Process(context);
                }
            }
        }
    }
}
