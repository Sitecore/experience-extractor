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
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Schema;
using Sitecore.ExperienceAnalytics.Aggregation.Dimensions;

namespace ExperienceExtractor.Components.Mapping.Sitecore
{
    public static class XaHelpers
    {
        public static IDimension GetDimension(string name)
        {            
            var type =
                typeof (DimensionBase).Assembly.GetType("Sitecore.ExperienceAnalytics.Aggregation.Dimensions." + name);

            return Activator.CreateInstance(type, new object[]{Guid.Empty}) as IDimension;
        }

        
        private static readonly MetricsCalculator _calculator = new MetricsCalculator();
        public static SegmentMetricsValue CalculateMetrics(IVisitAggregationContext context)
        {
            return _calculator.CalculateMetrics(context);
        }
        
        class MetricsCalculator : DimensionBase
        {
            public MetricsCalculator() : base(Guid.Empty)
            {
            }

            public override IEnumerable<DimensionData> GetData(IVisitAggregationContext context)
            {
                throw new NotImplementedException();
            }

            public SegmentMetricsValue CalculateMetrics(IVisitAggregationContext context)
            {
                return CalculateCommonMetrics(context, 1);
            }
        }
    }
}
