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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;

namespace ExperienceExtractor.Components.Mapping.Sitecore
{
    public class XaFacts : FieldMapperBase
    {
        public FactTypes FactTypes { get; set; }
        private readonly List<Field> _fields;

        public XaFacts(Func<string, string> nameFormatter = null, FactTypes factTypes = FactTypes.All)
        {
            FactTypes = factTypes;
            
            nameFormatter = nameFormatter ?? (name => name);
        
            _fields = new List<Field>();
            if( factTypes.HasFlag(FactTypes.Visits)) _fields.Add(new Field {Name = nameFormatter("Visits"), ValueType = typeof (int), FieldType = FieldType.Fact});
            if (factTypes.HasFlag(FactTypes.Value)) _fields.Add(new Field { Name = nameFormatter("Value"), ValueType = typeof(int), FieldType = FieldType.Fact });
            if (factTypes.HasFlag(FactTypes.Bounces)) _fields.Add(new Field { Name = nameFormatter("Bounces"), ValueType = typeof(int), FieldType = FieldType.Fact });
            if (factTypes.HasFlag(FactTypes.Conversions)) _fields.Add(new Field { Name = nameFormatter("Conversions"), ValueType = typeof(int), FieldType = FieldType.Fact });
            if (factTypes.HasFlag(FactTypes.TimeOnSite)) _fields.Add(new Field { Name = nameFormatter("TimeOnSite"), ValueType = typeof(int), FieldType = FieldType.Fact });
            if (factTypes.HasFlag(FactTypes.PageViews)) _fields.Add(new Field { Name = nameFormatter("PageViews"), ValueType = typeof(int), FieldType = FieldType.Fact });
            if (factTypes.HasFlag(FactTypes.Count)) _fields.Add(new Field { Name = nameFormatter("Count"), ValueType = typeof(int), FieldType = FieldType.Fact });            
        }


        protected override IEnumerable<Field> CreateFields()
        {
            return _fields;
        }

        public override bool SetValues(ProcessingScope scope, IList<object> row)
        {
            if (_fields.Count == 0) return false;

            var index = 0;
            
            //Get metrics from current DimensionData (EaTableMapper) or calculate from context

            var metrics = scope.Current<DimensionData>().TryGet(data => data.MetricsValue)
                          ?? scope.Current<IVisitAggregationContext>().TryGet(XaHelpers.CalculateMetrics);

            if (metrics != null)
            {
                if (FactTypes.HasFlag(FactTypes.Visits)) row[index++] = metrics.Visits;
                if (FactTypes.HasFlag(FactTypes.Value)) row[index++] = metrics.Value;
                if (FactTypes.HasFlag(FactTypes.Bounces)) row[index++] = metrics.Bounces;
                if (FactTypes.HasFlag(FactTypes.Conversions)) row[index++] = metrics.Conversions;
                if (FactTypes.HasFlag(FactTypes.TimeOnSite)) row[index++] = metrics.TimeOnSite;
                if (FactTypes.HasFlag(FactTypes.PageViews)) row[index++] = metrics.Pageviews;
                if (FactTypes.HasFlag(FactTypes.Count)) row[index++] = metrics.Count;                
               
                return true;
            }

            return false;
        }
    }

    [Flags]
    public enum FactTypes
    {
        Visits = 1,
        Value = 2,
        Bounces = 4,
        Conversions = 8,
        TimeOnSite = 16,
        PageViews = 32,
        Count = 64,
        All = 127
    }
}
