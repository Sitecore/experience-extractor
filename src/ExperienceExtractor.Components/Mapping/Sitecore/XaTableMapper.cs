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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;

namespace ExperienceExtractor.Components.Mapping.Sitecore
{
    public class XaTableMapper : TableMapperBase
    {
        public IDimension Dimension { get; set; }

        public XaTableMapper(IDimension dimension, string tableName = null,
            string dimensionTableName = null, bool hashKey = false,
            string keyName = null, string labelName = null, ILabelProvider labelProvider = null,            
            IEnumerable<IFieldMapper> additionalFields = null, FactTypes factTypes = FactTypes.All)
        {
            var defintion = new TableDefinition(tableName ?? dimension.GetType().Name);

            Dimension = dimension;

            labelName = labelName ?? dimension.GetType().Name + "Label";            
            
            defintion.FieldMappers.Add(new FieldMapperSet(dimensionTableName, dimensionTableName == null,
                new[]
                {
                    new LabeledFieldMapper(new XaDimensionDataMapper(dimension, !hashKey, keyName), labelName, labelProvider)
                }));

            defintion.FieldMappers.Add(new XaFacts(factTypes: factTypes));

            if (additionalFields != null)
            {
                foreach (var f in additionalFields)
                {
                    defintion.FieldMappers.Add(f);
                }
            }
            
            TableDefinitions.Add(defintion);
        }

        protected override IEnumerable SelectRowItems(ProcessingScope context)
        {
            var ctx = context.Current<IVisitAggregationContext>();
            if (ctx != null)
            {
                return Dimension.GetData(ctx).Where(d => d != null);
            }

            return Enumerable.Empty<object>();
        }


        class XaDimensionDataMapper : XaFieldMapper
        {
            public XaDimensionDataMapper(IDimension dimension, bool primaryKey = false, string keyName = null) : base(dimension, primaryKey, keyName)
            {

            }

            protected override DimensionData GetDimensionDataFromContext(ProcessingScope context)
            {
                return context.Current<DimensionData>();                
            }
        }
    }
}