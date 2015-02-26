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
using System.Data;
using System.Linq;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Export
{
    public static class DataTableHelpers
    {

        public static DataSet ToDataSet(this IEnumerable<TableData> tables, string tableNamePostfix = "")
        {
            var map = new Dictionary<TableDataSchema, DataTable>();
            var schema = new DataSet();
            foreach (var tableData in tables)
            {
                var table = tableData.ToDataTable(false, tableNamePostfix);
                map.Add(tableData.Schema, table);
                schema.Tables.Add(table);
            }

            foreach (var kv in map)
            {
                foreach(var relation in kv.Key.RelatedTables)
                {
                    if (relation.RelationType == RelationType.DimensionReference || relation.RelationType == RelationType.Child)
                    {                        
                        DataTable child;                        
                        if (map.TryGetValue(relation.RelatedTable, out child))
                        {
                            var parent = kv.Value;
                            var parentKeys = relation.Fields.Select(f =>
                                parent.Columns.Cast<DataColumn>().First(c => c.ExtendedProperties["XFieldPosition"] == f))
                                .ToArray();

                            var childKeys = relation.RelatedFields.Select(f =>
                                child.Columns.Cast<DataColumn>().First(c => c.ExtendedProperties["XFieldPosition"] == f))
                                .ToArray();

                            schema.Relations.Add(parentKeys, childKeys);
                        }
                    }
                }
            }
            

            return schema;
        }

        public static DataTable ToDataTable(this TableData data, bool fillData = true, string tableNamePostfix = "")
        {
            var table = new DataTable(data.Name + tableNamePostfix);
            var keys = new List<DataColumn>();
            foreach (var field in data.Schema.Fields)
            {
                var type = field.ValueType;
                var innerType = Nullable.GetUnderlyingType(type);

                var col = table.Columns.Add(field.Name, innerType ?? type);
                col.ExtendedProperties.Add("XFieldPosition", field);
                if (field.FieldType == FieldType.Key)
                {
                    keys.Add(col);
                }                         
            }
            table.PrimaryKey = keys.ToArray();

            if (fillData)
            {
                table.BeginLoadData();
                foreach (var row in data.Rows)
                {
                    var dataRow = table.NewRow();                    
                    for(var i = 0; i < data.Schema.Fields.Length; i++)
                    {
                        dataRow[i] = row[i];
                    }
                    table.Rows.Add(dataRow);
                }
                table.EndLoadData();
            }

            return table;
        }
    }
}