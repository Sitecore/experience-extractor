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
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping
{
    public class SimpleFieldMapper : FieldMapperBase
    {
        public string Name { get; set; }
        public Func<ProcessingScope, object> Selector { get; private set; }
        public Type ValueType { get; set; }
        public FieldType FieldType { get; set; }
        public SortOrder SortOrder { get; set; }

        public SimpleFieldMapper(string name, Func<ProcessingScope, object> selector, Type valueType, FieldType fieldType = FieldType.Dimension, SortOrder sortOrder = SortOrder.Unspecified)
        {
            Name = name;
            Selector = selector;
            ValueType = valueType;
            FieldType = fieldType;
            SortOrder = sortOrder;
        }


        protected override IEnumerable<Field> CreateFields()
        {
            yield return new Field {Name = Name, FieldType = FieldType, SortOrder = SortOrder, ValueType = ValueType};
        }

        public override bool SetValues(ProcessingScope scope, IList<object> row)
        {
            var val = Selector(scope);
            if (val != null)
            {
                row[0] = val;
                return true;
            }
            return false;
        }
    }
}