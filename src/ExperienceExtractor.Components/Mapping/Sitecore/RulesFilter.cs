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
using System.Linq.Expressions;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing.DataSources;
using Sitecore.Analytics;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;
using Sitecore.Data.Items;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;
using Sitecore.Rules;

namespace ExperienceExtractor.Components.Mapping.Sitecore
{
    public class RulesFilter : IDataFilter
    {
        private readonly RuleList<RuleContext> _rules;
                

        /// <summary>
        /// Used for RuleContext to allow conditions that expect an item to find one       
        /// </summary>        
        public Item RuleContextItem { get; set; }

        public RulesFilter(RuleList<RuleContext> rules)
        {            
            _rules = rules;        
        }

        public static RulesFilter FromString(string input)
        {
            return new RulesFilter(SegmentData.Deserialize(input));
        }        

        public bool Include(object item)
        {
            var visit = (item as IVisitAggregationContext).TryGet(v => v.Visit);
            if (visit != null)
            {
                var tracker = _trackerFactory(visit);
                TrackerSwitcher.Enter(tracker);
                try
                {

                    var rulesContext = new RuleContext();

                    rulesContext.Item = RuleContextItem;
                    _rules.Run(rulesContext);
                    object addToSegment;
                    var include = !rulesContext.IsAborted &&
                                  rulesContext.Parameters.TryGetValue("addVisit", out addToSegment) &&
                                  (bool) addToSegment;

                    return include;

                }
                finally
                {
                    if (Tracker.Current == tracker)
                    {
                        TrackerSwitcher.Exit();
                    }                    
                }
            }

            return false;
        }

        private static Func<VisitData, ITracker> _trackerFactory;

        static RulesFilter()
        {
            var type = typeof(DimensionData).Assembly.GetType("Sitecore.ExperienceAnalytics.Aggregation.Rules.AggregationAdaptor.AggregationAdaptorTracker");

            var ctorInfo = type.GetConstructor(new[] { typeof(VisitData) });

            var p = Expression.Parameter(typeof(VisitData));
            var ctor = Expression.New(ctorInfo, p);

            _trackerFactory = Expression.Lambda<Func<VisitData, ITracker>>(ctor, p).Compile();
        }
    }
}
