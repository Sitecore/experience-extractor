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

using ExperienceExtractor.Processing.DataSources;
using Sitecore.Analytics.Aggregation.Data.Model;

namespace ExperienceExtractor.Processing.DateTime
{
    public class DateRangeFilter : TypedDataFilter<IVisitAggregationContext>
    {
        public System.DateTime? Start { get; set; }
        public System.DateTime? End { get; set; }        

        public DateRangeFilter(System.DateTime? start = null, System.DateTime? end = null)
        {
            Start = start;
            End = end;        
        }

        public override bool IsStagingFilter { get { return true; } }

        protected override bool Include(IVisitAggregationContext item)
        {
            if (item == null) return false;
            if (Start.HasValue && item.Visit.StartDateTime < Start) return false;
            if (End.HasValue && item.Visit.StartDateTime >= End) return false;

            return true;
        }        
    }
}
