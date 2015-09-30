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
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Components.PostProcessors;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using Sitecore.Analytics.Aggregation.Data.Model;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("PartitionKey", "Partition field", "Used for incremental updates. The timespan defines a tumbling window, and should be set to a value greater than the max latency time expected for the datastore queried for interactions. (Now() - SaveDateTime).")]
    public class PartitionFieldFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var staleTime = TimeSpan.Parse(state.Require<string>("MaxStaleTime", true));

            return
                new SimpleFieldMapper(
                    s =>
                        SqlUpdateUtil.GetPartitionDate(s.Current<IVisitAggregationContext>().Visit.SaveDateTime.ToUniversalTime(),
                            staleTime),
                    new PartitionField
                    {
                        Name = "Partition",
                        FieldType = FieldType.Dimension,
                        StaleTime = staleTime,
                        ValueType = typeof (DateTime)
                    });
        }
    }
}
