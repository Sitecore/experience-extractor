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
using System.Data.OleDb;
using System.IO;
using System.Linq;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Export;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;
using ExperienceExtractor.Processing.Helpers;
using Sitecore.Tasks;

namespace ExperienceExtractor.Components.PostProcessors
{
    public class AccessExporter : ITableDataPostProcessor
    {           
        public string Name { get { return "Access"; }}
        public void Process(string tempDirectory, IEnumerable<TableData> tables, IJobSpecification job)
        {

            if (tables.Any(t => (t as CsvTableData) == null))
            {
                throw new NotSupportedException("AccessExporter require CsvTableData");
            }

            var csvTables = tables.Cast<CsvTableData>();

            var path = Path.Combine(tempDirectory, "Result.accdb");
            using (var file = File.Create(path))
            using (var templateDb = typeof(AccessExporter).Assembly.GetManifestResourceStream("ExperienceExtractor.Components.Resources.Empty.accdb"))
            {
                templateDb.CopyTo(file);
            }
            
            using (var conn = new OleDbConnection(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path))
            {
                conn.Open();

                foreach (var table in csvTables)
                {
                    var csvFile = new FileInfo(table.Path);
                    
                    new OleDbCommand(
                        string.Format(
                            @"SELECT * INTO [{0}] FROM [Text;DATABASE={1}].{2}",
                            table.Name, csvFile.DirectoryName, csvFile.Name), conn)
                        .ExecuteNonQuery();
                }


                foreach (var t in tables)
                {
                    var pks = t.Schema.Keys;
                    if (pks.Length > 0)
                    {
                        new OleDbCommand(
                            string.Format(@"ALTER TABLE [{0}] ADD PRIMARY KEY ({1})",
                                t.Name, FieldList(pks)),
                            conn).ExecuteNonQuery();
                    }
                }

                var nextId = 1;
                foreach (var table in tables)
                {
                    foreach (
                        var rel in table.Schema.RelatedTables.Where(
                                        r =>
                                            r.RelationType == RelationType.Parent ||
                                            r.RelationType == RelationType.Dimension))
                    {                        
                        new OleDbCommand(
                            string.Format(
                                @"ALTER TABLE [{0}] ADD CONSTRAINT {4} FOREIGN KEY ({1}) REFERENCES [{2}] ({3})",
                                table.Name, FieldList(rel.Fields), rel.RelatedTable.Name, FieldList(rel.RelatedFields), "FK" + nextId++), conn).ExecuteNonQuery();
                    }
                }

            }
        }

        public void Validate(IEnumerable<TableData> tables, IJobSpecification job)
        {
            
        }

        public bool UpdateDataSource(IEnumerable<TableData> tables, IDataSource source)
        {
            return false;
        }        

        private string FieldList(IEnumerable<Indexed<Field>> fields)
        {
            return string.Join(",", fields.Select(f => "[" + f.Value.Name + "]"));
        }

        private string FieldList(IEnumerable<Field> fields)
        {
            return string.Join(",", fields.Select(f => "[" + f.Name + "]"));
        }
    }
}