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

namespace ExperienceExtractor.Data.Schema
{
    /// <summary>
    /// Defines a field in a table
    /// </summary>
    public class Field : ICloneable
    {        
        /// <summary>
        /// The name of the field in the exported dataset
        /// </summary>
        public string Name { get; set; }


        /// <summary>
        /// A friendly name for the field for client applications
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// The field's .NET data type. Nullable is used to indicate that a field with a value type can be null
        /// </summary>
        public Type ValueType { get; set; }

        /// <summary>
        /// The role of the field in the table.
        /// </summary>
        public FieldType FieldType { get; set; }

        /// <summary>
        /// Specifies if rows should be sorted by the field in the output
        /// </summary>
        public SortOrder SortOrder { get; set; }
        
        /// <summary>
        /// Hints that this field should be sorted by the value of another field in the table with this name.
        /// For instance month names should be sorted by month number.
        /// </summary>
        public string SortBy { get; set; }

        /// <summary>
        /// The field's default value. If a non nullable value type is used, the default value is automatically the default value for this type (e.g. 0 for Int32)
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        /// Used to indicate that this fields contains, e.g., "Visits". Used for resolving field references in calculated fields
        /// </summary>
        public string ValueKind { get; set; }

        /// <summary>
        /// Hide the field (surrogate key)
        /// </summary>
        public bool Hide { get; set; }

        
        /// <summary>
        /// Creates a copy of the field with the field type specified. Value types are made nullable to allow null references in tables.
        /// </summary>
        /// <param name="type">The field type for the clone</param>
        /// <returns></returns>
        public Field AsNullableReference(FieldType type)
        {
            var clone = Clone();
            var valueType = ValueType;
            if (valueType.IsValueType)
            {
                clone.ValueType = typeof(Nullable<>).MakeGenericType(Nullable.GetUnderlyingType(valueType) ?? valueType);
            }
            clone.FieldType = type;
            return clone;
        }

        public Field ChangeSort(SortOrder sortOrder)
        {
            var clone = Clone();
            clone.SortOrder = sortOrder;
            return clone;
        }

        /// <summary>
        /// Creates a copy of the field with its name updated by the function specified
        /// </summary>
        /// <param name="nameFormatter">A function that maps the field name to a new name</param>
        /// <returns></returns>
        public Field Affix(Func<string, string> nameFormatter = null)
        {
            if (nameFormatter == null) return this;

            var clone = Clone();
            clone.Name = nameFormatter(clone.Name);

            return clone;
        }        

        /// <summary>
        /// Creates a clone of the field
        /// </summary>
        /// <returns></returns>
        public Field Clone()
        {
            return new Field
            {
                Name = Name,
                ValueType = ValueType,
                FieldType = FieldType,
                SortOrder = SortOrder,
                DefaultValue = DefaultValue,                
                SortBy = SortBy,
                Hide = Hide,
                FriendlyName = FriendlyName
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        protected bool Equals(Field other)
        {
            return string.Equals(Name, other.Name) && ValueType == other.ValueType && FieldType == other.FieldType && SortOrder == other.SortOrder && string.Equals(SortBy, other.SortBy);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Field)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ValueType != null ? ValueType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)FieldType;
                hashCode = (hashCode * 397) ^ (int)SortOrder;
                hashCode = (hashCode * 397) ^ (SortBy != null ? SortBy.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}