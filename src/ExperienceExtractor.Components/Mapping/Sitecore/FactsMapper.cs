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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.Helpers;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;
using Sitecore.ExperienceAnalytics.Aggregation.Dimensions;

namespace ExperienceExtractor.Components.Mapping.Sitecore
{
    public class FactsMapper : FieldMapperBase
    {

        public FactTypes FactTypes { get; set; }
        public Func<string, string> NameFormatter { get; set; }
        public bool CalculateForEntireVisit { get; set; }

        public FactsMapper(FactTypes factTypes, Func<string, string> nameFormatter, bool calculateForEntireVisit = false)
        {
            NameFormatter = nameFormatter;
            CalculateForEntireVisit = calculateForEntireVisit;
            FactTypes = factTypes;
        }

        protected override IEnumerable<Field> CreateFields()
        {
            if (FactTypes.HasFlag(FactTypes.Visits)) yield return new Field { Name = NameFormatter("Visits"), ValueType = typeof(int), FieldType = FieldType.Fact };
            if (FactTypes.HasFlag(FactTypes.Value)) yield return new Field { Name = NameFormatter("Value"), ValueType = typeof(int), FieldType = FieldType.Fact };
            if (FactTypes.HasFlag(FactTypes.Bounces)) yield return new Field { Name = NameFormatter("Bounces"), ValueType = typeof(int), FieldType = FieldType.Fact };
            if (FactTypes.HasFlag(FactTypes.Conversions)) yield return new Field { Name = NameFormatter("Conversions"), ValueType = typeof(int), FieldType = FieldType.Fact };
            if (FactTypes.HasFlag(FactTypes.TimeOnSite)) yield return new Field { Name = NameFormatter("TimeOnSite"), ValueType = typeof(int), FieldType = FieldType.Fact };
            if (FactTypes.HasFlag(FactTypes.PageViews)) yield return new Field { Name = NameFormatter("PageViews"), ValueType = typeof(int), FieldType = FieldType.Fact };
            if (FactTypes.HasFlag(FactTypes.Count)) yield return new Field { Name = NameFormatter("Count"), ValueType = typeof(int), FieldType = FieldType.Fact };        
        }

        public override bool SetValues(ProcessingScope scope, IList<object> row)
        {
            var ce = CalculateForEntireVisit ? null : scope.Current<PageEventData>();
            var cp = CalculateForEntireVisit ? null : scope.Current<PageData>();
            var cv = scope.Current<IVisitAggregationContext>().TryGet(v=>v.Visit);                        

            if (cv == null) return false;

            var es = ce != null ? new[] { ce } : cp != null ? cp.PageEvents.OrEmpty() : cv.Pages.OrEmpty().SelectMany(p => p.PageEvents.OrEmpty());
            var ps = cp != null ? new[] { cp } : cv.Pages.OrEmpty();

            es = es.Where(e => e.IsGoal);

            var index = 0;
            if (FactTypes.HasFlag(FactTypes.Visits)) row[index++] = scope.OncePerScope<IVisitAggregationContext>(1);
            if (FactTypes.HasFlag(FactTypes.Value)) row[index++] =  es.Sum(e => e.Value);
            if (FactTypes.HasFlag(FactTypes.Bounces)) row[index++] = cv.Pages.TryGet(_ => _.Count == 1 ? 1 : 0);
            if (FactTypes.HasFlag(FactTypes.Conversions)) row[index++] = es.Count();
            if (FactTypes.HasFlag(FactTypes.TimeOnSite))
                row[index++] = ps.Sum(p => DimensionBase.ConvertDuration(p.Duration));
            if (FactTypes.HasFlag(FactTypes.PageViews)) row[index++] = cp != null ? scope.OncePerScope<PageData>(1) : ps.Count();

            if (CalculateForEntireVisit)
            {
                for (var i = 0; i < index; i++)
                {
                    if (!(row[i] is IDeferedValue))
                    {
                        row[i] = scope.OncePerScope<IVisitAggregationContext>(row[i]);
                    }
                }
            }

            if (FactTypes.HasFlag(FactTypes.Count)) row[index++] = 1;


            return true;
        }
    }
}
