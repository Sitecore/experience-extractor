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
using ExperienceExtractor.Mapping.Splitting;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;

namespace ExperienceExtractor.Components.Mapping.Splitting
{
    public class PageConditionSplitter : TypedDataFilter<IVisitAggregationContext>, ISplitter
    {
        public Func<ProcessingScope, PageData, bool>[] Conditions { get; set; }
        public bool IncludeTotal { get; set; }
        public bool IncludeMatchBefore { get; set; }
        public bool EmptyIfConditionNotMet { get; set; }
        public string[] Names { get; private set; }


        public PageConditionSplitter(Func<ProcessingScope, PageData, bool> condition, bool includeMatchBefore = true, bool emptyIfConditionNotMet = true, bool includeTotal = true)
            : this(new[] { condition }, new[] { "Before", "After" }, includeMatchBefore, emptyIfConditionNotMet, includeTotal)
        {
                        
        }

        public PageConditionSplitter(IEnumerable<Func<ProcessingScope, PageData, bool>> conditions, IEnumerable<string> names, bool includeMatchBefore = true,
            bool emptyIfConditionNotMet = true, bool includeTotal = true)
        {
            Conditions = conditions.ToArray();
            IncludeTotal = includeTotal;
            if (includeTotal)
            {
                names = names.Concat(new[] {"Total"});
            }
            Names = names.ToArray();

            IncludeMatchBefore = includeMatchBefore;
            EmptyIfConditionNotMet = emptyIfConditionNotMet;
        }


        public IEnumerable<object> GetSplits(ProcessingScope scope)
        {
            var visitContext = scope.Current<IVisitAggregationContext>();
            var vd = visitContext.Visit;

            var originalVisitPageCount = vd.VisitPageCount;
            var originalValue = vd.Value;
            var all = vd.Pages.ToList();

            var lists = Enumerable.Range(0, Conditions.Length + 1).Select(c => new List<PageData>()).ToArray();
            
            var targetIndex = 0;
            foreach (var page in all)
            {
                if( IncludeMatchBefore ) lists[targetIndex].Add(page);
                if (targetIndex < Conditions.Length && Conditions[targetIndex](scope, page))
                {
                    ++targetIndex;
                }
                if (!IncludeMatchBefore) lists[targetIndex].Add(page);
            }

            if (targetIndex < lists.Length - 1 && EmptyIfConditionNotMet)
            {
                //All conditions were not met. Clear all lists
                foreach (var list in lists)
                {
                    list.Clear();                    
                }
            }

            var vals = new int[lists.Length];

            var ix = 0;
            foreach (var list in lists)
            {                
                vd.Pages = list;
                vd.VisitPageCount = list.Count;
                vd.Value = visitContext.Visit.Pages.Sum(p => p.PageEvents.Sum(pe => pe.Value));
                vals[ix++] = vd.Value;
                yield return visitContext;
            }

            vd.Pages = all;
            vd.VisitPageCount = originalVisitPageCount;
            vd.Value = originalValue;
            

            if (IncludeTotal)
            {                
                yield return visitContext;
            }            
        }

        protected override bool Include(IVisitAggregationContext item)       
        {            
            if (item == null) return false;
            
            var conditionIndex = 0;            
            foreach (var page in item.Visit.Pages)
            {                
                if (Conditions[conditionIndex](null, page))
                {                    
                    if (++conditionIndex >= Conditions.Length)
                    {
                        //The last condition matched a page
                        return true;
                    }
                }
            }
            return false;

        }
    }
}
