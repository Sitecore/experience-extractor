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
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ExperienceExtractor.Data.Schema
{
    public static class TableDataHelpers
    {

        public static bool IsCenterTable(this TableDataSchema table)
        {
            return table.RelatedTables.All(
                r => r.RelationType == RelationType.Dimension || r.RelationType == RelationType.Child);
        }

        public static bool IsKey(this TableDataSchema table, Field field)
        {
            return table.Keys.Length == 0 ? field.FieldType == FieldType.Dimension : field.FieldType == FieldType.Key;            
        }

        public static bool IsDimension(this TableDataSchema table)
        {
            return table.RelatedTables.All(
                r => r.RelationType == RelationType.DimensionReference);
        }

        public static string Serialize(this IEnumerable<TableDataSchema> schemas)
        {
            return JsonConvert.SerializeObject(schemas,
                new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Objects
                });
        }

        public static TableDataSchema[] Deserialize(string rep)
        {
            var schemas = JsonConvert.DeserializeObject<TableDataSchema[]>(rep,
                new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects, TypeNameHandling = TypeNameHandling.Objects });

            //Restore nested references. For some reason these are null. TODO: Find out how JSON.NET handles this.
            foreach (var s in schemas)
            {
                foreach (var r in s.RelatedTables.Where(r => r.RelatedTable == null))
                {
                    r.RelatedTable = schemas.First(ss => ss.RelatedTables.Any(rr => rr.Fields.SequenceEqual(r.RelatedFields)));
                }
            }

            return schemas;
        }

        public static bool FieldsAreEqual(this IEnumerable<TableDataSchema> left, IEnumerable<TableDataSchema> right)
        {
            var map = right.ToDictionary(r => r.Name);
            foreach (var table in left)
            {
                TableDataSchema other;
                if (!map.TryGetValue(table.Name, out other))
                {
                    return false;
                }

                if (!table.FieldsAreEqual(other)) return false;
            }
            return true;
        }
    }
}
