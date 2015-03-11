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
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Export;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Components.PostProcessors
{
    /// <summary>
    /// Post processor that exports table data to a SQL server.
    /// Database and schema can be created automatically
    /// </summary>
    public class SqlExporter : ITableDataPostProcessor
    {
        public string ConnectionString { get; set; }
        public string CreateDatabaseName { get; set; }

        public int Timeout { get; set; }

        public string Name { get { return "Sql Server"; } }
        public void Process(string tempDirectory, IEnumerable<CsvTableData> tables)
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString);

            using (var conn = new SqlConnection(builder.ConnectionString))
            {
                conn.Open();

                if (!string.IsNullOrEmpty(CreateDatabaseName))
                {
                    //Drop the database if it already exists
                    try
                    {
                        new SqlCommand(string.Format(@"ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE [{0}]", CreateDatabaseName), conn).ExecuteNonQuery();
                    }
                    catch { }

                    //Create the database, and set it to simple recovery mode. This makes inserts faster.
                    new SqlCommand(string.Format("CREATE DATABASE [{0}];", CreateDatabaseName), conn).ExecuteNonQuery();
                    new SqlCommand(string.Format("ALTER DATABASE [{0}] SET RECOVERY SIMPLE", CreateDatabaseName), conn).ExecuteNonQuery();
                    conn.ChangeDatabase(CreateDatabaseName);

                    var s = new StringWriter();
                    WriteSchema(s, tables);
                    new SqlCommand(s.ToString(), conn).ExecuteNonQuery();
                }

                foreach (var table in tables)
                {
                    new SqlCommand(string.Format("DELETE FROM {0}", table.Name), conn)
                    {
                        CommandTimeout = Timeout
                    }.ExecuteNonQuery();

                    using (var bcp = new SqlBulkCopy(conn))
                    {
                        bcp.BulkCopyTimeout = Timeout;
                        bcp.DestinationTableName = table.Name;
                        bcp.WriteToServer(table.CreateReader());
                    }
                }
            }
        }

        public void Validate()
        {
            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();
            }
        }

        public SqlExporter(string connectionString, string createDatabaseName = null)
        {
            ConnectionString = connectionString;
            CreateDatabaseName = createDatabaseName;
            Timeout = 3600 * 6;
        }


        void WriteSchema(TextWriter writer, IEnumerable<TableData> tables)
        {
            foreach (var table in tables.Reverse())
            {
                writer.WriteLine(@"IF OBJECT_ID('dbo.[{0}]', 'U') IS NOT NULL DROP TABLE dbo.[{0}];", table.Name);
            }

            foreach (var table in tables)
            {
                var i = 0;
                writer.Write("CREATE TABLE [{0}] (", table.Name);
                foreach (var field in table.Schema.Fields)
                {
                    writer.WriteLine(i++ > 0 ? "," : "");
                    writer.Write("    [{0}] {1} {2}", field.Name,
                        GetColumnType(field.ValueType, field.FieldType == FieldType.Key ? 255 : (int?)null),
                        field.FieldType == FieldType.Key || (field.ValueType.IsValueType && Nullable.GetUnderlyingType(field.ValueType) == null) ? "NOT NULL" : "NULL");
                }

                if (table.Schema.Keys.Length > 0)
                {
                    writer.WriteLine(i++ > 0 ? "," : "");
                    writer.Write("    PRIMARY KEY({0})", string.Join(",", table.Schema.Keys.Select(pos => string.Format("[{0}]", pos.Value.Name))));
                }
                writer.WriteLine();


                writer.WriteLine(")");
                writer.WriteLine();
            }

            foreach (var table in tables)
            {
                foreach (
                    var rel in table.Schema.RelatedTables.Where(
                                    r =>
                                        r.RelationType == RelationType.Parent ||
                                        r.RelationType == RelationType.Dimension))
                {

                    writer.WriteLine(
                        @"ALTER TABLE [{0}] ADD FOREIGN KEY ({1}) REFERENCES [{2}] ({3});",
                        table.Name, FieldList(rel.Fields), rel.RelatedTable.Name, FieldList(rel.RelatedFields));
                }
            }
        }


        private string FieldList(IEnumerable<Field> fields)
        {
            return string.Join(",", fields.Select(f => "[" + f.Name + "]"));
        }

        static string GetColumnType(Type t, int? maxLength = null)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;

            var name = _sqlTypes[t];
            if (t == typeof(string) || t == typeof(byte[]))
            {
                name += string.Format("({0})", maxLength.HasValue && maxLength > 0 ? maxLength + "" : "max");
            }

            return name;
        }


        private static Dictionary<Type, string> _sqlTypes = new Dictionary<Type, string>
        {
            {typeof (long), "bigint"},
            {typeof (byte[]), "varbinary"},
            {typeof (bool), "bit"},
            {typeof (string), "nvarchar"},
            {typeof (DateTime), "datetime2"},
            {typeof (decimal), "decimal(18,2)"},
            {typeof (float), "float"},
            {typeof (int), "int"},
            {typeof (Guid), "uniqueidentifier"},
        };



    }
}