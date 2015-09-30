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
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping
{
    public class SimpleFieldMapper : FieldMapperBase
    {        
        public Field Field { get; set; }

        public string Name
        {
            get { return Field.Name; }
        }

        public Func<ProcessingScope, object> Selector { get; private set; }
        public Type ValueType { get; set; }
        public FieldType FieldType { get; set; }
        public SortOrder SortOrder { get; set; }
        public string SortBy { get; set; }
        public string ValueKind { get; set; }
        public bool Hide { get; set; }

        public List<CalculatedField> CalculatedFieldList { get; set; }

        public SimpleFieldMapper(string name, Func<ProcessingScope, object> selector, Type valueType, FieldType fieldType = FieldType.Dimension, SortOrder sortOrder = SortOrder.Unspecified, string sortBy = null, IEnumerable<CalculatedField> calculatedFields = null, string valueKind = null, bool hide = false)
             : this(selector, new Field
            {
                Name = name,
                ValueType = valueType,
                FieldType = fieldType,
                SortOrder = sortOrder,
                SortBy = sortBy,
                ValueKind = valueKind,
                Hide = hide
            }, calculatedFields)
        {
            //TODO: Refactor so that all simple field mappers' constructs a field rather than using this bloated constructor.
        }

        public SimpleFieldMapper(Func<ProcessingScope, object> selector, Field field, IEnumerable<CalculatedField> calculatedFields = null)
        {
            Selector = selector;
            Field = field;

            if (calculatedFields != null)
            {
                CalculatedFieldList = calculatedFields.ToList();
            }
        }


        protected override IEnumerable<Field> CreateFields()
        {
            if (Field != null)
            {
                yield return Field;
                yield break;
            }

            yield return new Field { Name = Name, FieldType = FieldType, SortOrder = SortOrder, ValueType = ValueType, SortBy = SortBy, ValueKind = ValueKind, Hide = Hide };
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

        public override IEnumerable<CalculatedField> CalculatedFields
        {
            get { return CalculatedFieldList ?? base.CalculatedFields; }
        }
    }
}