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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping.Time;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping
{
    public static class Tables
    {

        public static TableDefinition Map(this TableDefinition def, IFieldMapper field)
        {
            def.FieldMappers.Add(field);
            return def;
        }

        public static TableDefinition Map(this TableDefinition def, Func<ProcessingScope, IEnumerable> selector, params TableDefinition[] childTables)
        {
            def.TableMappers.Add(new SimpleTableMapper(selector, childTables));
            return def;
        }

        public static TableDefinition Map(this TableDefinition def, ITableMapper table)
        {
            def.TableMappers.Add(table);
            return def;
        }
        
        public static TableDefinition Key<TValue>(this TableDefinition def, string name, Func<ProcessingScope, TValue> selector, SortOrder sort = SortOrder.Unspecified)
        {
            def.FieldMappers.Add(new SimpleFieldMapper(name, o => selector(o), typeof(TValue), FieldType.Key, sort));
            return def;
        }

        public static TableDefinition Dimension<TValue>(this TableDefinition def, string name, Func<ProcessingScope, TValue> selector, SortOrder sort = SortOrder.Unspecified)
        {
            def.FieldMappers.Add(new SimpleFieldMapper(name, o => selector(o), typeof(TValue), FieldType.Dimension, sort));
            return def;
        }

        public static TableDefinition Fact<TValue>(this TableDefinition def, string name, Func<ProcessingScope, TValue> selector, SortOrder sort = SortOrder.Unspecified)
        {
            def.FieldMappers.Add(new SimpleFieldMapper(name, o => selector(o), typeof(TValue), FieldType.Fact, sort));
            return def;
        }

        public static TableDefinition CountDistinct<TType>(this TableDefinition def, string name) where TType : class
        {
            def.FieldMappers.Add(new SimpleFieldMapper(name, o => o.OncePerScope<TType>(1), typeof(int), FieldType.Fact));
            return def;
        }

        public static TableDefinition Count(this TableDefinition def, string name = "Count")
        {
            def.FieldMappers.Add(new SimpleFieldMapper(name, o => 1, typeof(int), FieldType.Fact));
            return def;
        }

        public static TableDefinition Index<TType>(this TableDefinition def, string name = "Index", SortOrder sort = SortOrder.Unspecified, FieldType type = FieldType.Dimension) where TType : class
        {
            def.FieldMappers.Add(new SimpleFieldMapper(name, o => o.Index<TType>(), typeof(int), type, sort));
            return def;
        }
    }
}
