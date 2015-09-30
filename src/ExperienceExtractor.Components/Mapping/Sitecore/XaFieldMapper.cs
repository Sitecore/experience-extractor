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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;

namespace ExperienceExtractor.Components.Mapping.Sitecore
{
    public class XaFieldMapper : FieldMapperBase
    {
        public IDimension Dimension { get; set; }

        private string _keyName;        
        private readonly bool _primaryKey;
        private readonly string _friendlyName;

        public XaFieldMapper(IDimension dimension,
            bool primaryKey = false,
            string keyName = null, string friendlyName = null)
        {
            Dimension = dimension;

            _keyName = keyName ?? dimension.GetType().Name + "Key";
            _primaryKey = primaryKey;
            _friendlyName = friendlyName ?? SuggestFriendlyKeyName(_keyName);
        }


        protected override IEnumerable<Field> CreateFields()
        {
            yield return
                new Field
                {
                    FieldType = _primaryKey ? FieldType.Key : FieldType.Dimension,
                    Name = _keyName,
                    FriendlyName = _friendlyName,
                    ValueType = typeof (string)
                };
            
        }

        public override bool SetValues(ProcessingScope scope, IList<object> row)
        {
            var dimensionData = GetDimensionDataFromContext(scope);
            if (dimensionData != null)
            {
                row[0] = dimensionData.DimensionKey;                
                return true;
            }

            return false;
        }


        protected virtual DimensionData GetDimensionDataFromContext(ProcessingScope context)
        {
            var ctx = context.Current<IVisitAggregationContext>();
            if (ctx != null)
            {
                return Dimension.GetData(ctx).FirstOrDefault();
            }
            return null;
        }


        public static string SuggestFriendlyKeyName(string keyName)
        {
            if (keyName.StartsWith("By"))
            {
                keyName = keyName.Substring(2);
            }

            if (keyName.EndsWith("Key"))
            {
                keyName = keyName.Substring(0, keyName.Length - 3) + " ID";
            }

            return keyName;
        }

        public static string SuggestFriendlyLabelName(string keyName)
        {
            if (keyName.StartsWith("By"))
            {
                keyName = keyName.Substring(2);
            }

            if (keyName.EndsWith("Label"))
            {
                keyName = keyName.Substring(0, keyName.Length - 5);
            }

            return keyName;
        }
    }
}