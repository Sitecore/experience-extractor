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
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;
using Sitecore.Common;

namespace ExperienceExtractor.Components.PostProcessors
{
    /// <summary>
    /// Post processor that exports table data to a SQL server.
    /// Database and schema can be created automatically, and data can be updated
    /// </summary>
    public class SqlExporter : IUpdatingTableDataPostProcessor
    {
        public string ConnectionString { get; set; }
        public string CreateDatabaseName { get; set; }
        public bool ClearInsteadOfDropCreate { get; set; }

        public int Timeout { get; set; }

        public string Name { get { return "Sql Server"; } }

        public string SsasConnectionString { get; set; }
        public string SsasDbName { get; set; }

        public bool Update { get; set; }

        public bool UseStagingTables { get; set; }

        //TODO: Debug thing. Remove.
        public bool SsasOnly { get; set; }

        public SqlClearOptions SqlClearOptions { get; set; }

        public bool Rebuild { get; set; }

        public event EventHandler<SqlTransaction> BeforeCommit;
        public event EventHandler<SqlConnection> SchemaCreating;

        public SqlExporter(string connectionString, string createDatabaseName = null, bool clearInsteadOfDropCreate = false)
        {
            ConnectionString = connectionString;
            CreateDatabaseName = createDatabaseName;
            ClearInsteadOfDropCreate = clearInsteadOfDropCreate;
            Timeout = 3600 * 6;
            SqlClearOptions = SqlClearOptions.All;
        }

        public void Process(string tempDirectory, IEnumerable<TableData> tables, IJobSpecification job)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");

            using (var conn = new SqlConnection(this.ConnectionString))
            {
                conn.Open();

                if (!this.SsasOnly)
                {
                    if (!string.IsNullOrEmpty(this.CreateDatabaseName))
                    {
                        if (!this.Update)
                        {
                            if (!this.ClearInsteadOfDropCreate)
                            {
                                //Drop the database if it already exists
                                try
                                {
                                    new SqlCommand(string.Format(@"ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                            DROP DATABASE [{0}]", this.CreateDatabaseName), conn).ExecuteNonQuery();
                                }
                                catch
                                {
                                }

                                //Create the database, and set it to simple recovery mode. This makes inserts faster.
                                new SqlCommand(string.Format("CREATE DATABASE [{0}];", this.CreateDatabaseName), conn)
                                    .ExecuteNonQuery();
                                new SqlCommand(
                                    string.Format("ALTER DATABASE [{0}] SET RECOVERY SIMPLE", this.CreateDatabaseName),
                                    conn).ExecuteNonQuery();
                            }

                            conn.ChangeDatabase(this.CreateDatabaseName);

                            if (this.ClearInsteadOfDropCreate)
                            {
                                DropAllConstraints(conn);
                                DropAllTables(conn);
                                DropAllUserDefinedTypes(conn);
                            }

                            if((int) new SqlCommand(@"SELECT count(*) WHERE schema_id('Staging') IS NOT NULL", conn).ExecuteScalar() <= 0)
                            {
                                new SqlCommand(@"CREATE SCHEMA Staging;", conn).ExecuteNonQuery();
                            }

                            var s = new StringWriter();
                            this.WriteSchema(s, tables);
                            new SqlCommand(s.ToString(), conn).ExecuteNonQuery();

                            if ((int)new SqlCommand(@"SELECT count(*) WHERE schema_id('Sitecore') IS NOT NULL", conn).ExecuteScalar() <= 0)
                            {
                                new SqlCommand(@"CREATE SCHEMA Sitecore;", conn).ExecuteNonQuery();
                            }

                            new SqlCommand(
                                @"CREATE TABLE Sitecore.JobInfo ( [Schema] nvarchar(max), [Prototype] nvarchar(max), [LockDate] datetime2 null, LastCutoff datetime2 null );",
                                conn).ExecuteNonQuery();
                            var cmd = new SqlCommand(@"INSERT Sitecore.JobInfo ([Schema], [Prototype]) VALUES (@Schema, @Prototype)", conn);
                            cmd.Parameters.AddWithValue("@Schema", tables.Select(t => t.Schema).Serialize());
                            cmd.Parameters.AddWithValue("@Prototype", job.ToString());
                            cmd.ExecuteNonQuery();

                            this.OnSchemaCreating(conn);
                        }
                        else
                        {
                            conn.ChangeDatabase(this.CreateDatabaseName);

                            this.ValidateSchema(conn, tables);
                        }
                    }

                    this.AcquireLock(conn, null); //TODO: Implement IDisposable...
                    try
                    {
                        new SqlCommand(@"EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all'", conn)
                            .ExecuteNonQuery();

                        try
                        {
                            this.UseStagingTables = true;

                            if (this.UseStagingTables)
                            {
                                Task.WaitAll(tables.Select(table => Task.Run(() => this.UploadData(table))).ToArray());
                            }

                            using (var tran = conn.BeginTransaction())
                            {
                                foreach (var table in tables)
                                {
                                    this.InsertOrUpdateData(table, conn, tran,
                                        this.UseStagingTables ? GetStagingTableName(table) : null);
                                }

                                var cmd = new SqlCommand(@"UPDATE Sitecore.JobInfo SET LastCutoff=@LastCutoff", conn, tran);
                                cmd.Parameters.AddWithValue(@"LastCutoff", this._nextCuttoff ?? (object)DBNull.Value);
                                cmd.ExecuteNonQuery();

                                this.OnBeforeCommit(tran);

                                tran.Commit();
                            }

                            if (this.UseStagingTables)
                            {
                                foreach (var table in tables)
                                {
                                    new SqlCommand(
                                        string.Format(@"TRUNCATE TABLE {0}", GetStagingTableName(table)), conn)
                                    {
                                        CommandTimeout = this.Timeout
                                    }.ExecuteNonQuery();
                                }
                            }
                        }
                        finally
                        {
                            new SqlCommand(@"EXEC sp_msforeachtable 'ALTER TABLE ? CHECK CONSTRAINT all'", conn)
                                .ExecuteNonQuery();
                        }

                        if (this._ssasExporter != null)
                        {
                            this._ssasExporter.CutOff = this._cutoff;
                            this._ssasExporter.Process(tempDirectory, tables, job);
                        }
                    }
                    finally
                    {
                        this.ReleaseLock(conn, null);
                    }
                }
                else
                {
                    if (this._ssasExporter != null)
                    {
                        this._ssasExporter.Process(tempDirectory, tables, job);
                    }
                }
            }
        }

        private SsasExporter _ssasExporter;
        private DateTime? _cutoff;
        private DateTime? _nextCuttoff;
        public void Validate(IEnumerable<TableData> tables, IJobSpecification job)
        {
            var hasPartitionKey = false;
            var staleTime = GetStaleTime(tables);

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                if (!string.IsNullOrEmpty(CreateDatabaseName))
                {

                    if (Update)
                    {
                        conn.ChangeDatabase(CreateDatabaseName);

                        //Check lock. Release immediately upon success.
                        AcquireLock(conn, null);
                        ReleaseLock(conn, null);

                        ValidateSchema(conn, tables);
                    }

                    if (staleTime > TimeSpan.Zero)
                    {
                        hasPartitionKey = true;
                        if (Update && !Rebuild)
                        {
                            var lastCutoff =
                                new SqlCommand(@"SELECT TOP 1 LastCutoff FROM Sitecore.JobInfo", conn).ExecuteScalar();
                            _cutoff = DBNull.Value.Equals(lastCutoff) ? (DateTime?)null : ((DateTime)lastCutoff).SpecifyKind(DateTimeKind.Utc);
                            if (_cutoff.HasValue)
                            {
                                _cutoff = SqlUpdateUtil.GetPartitionDate(_cutoff.Value.Add(-staleTime), staleTime);
                            }
                        }

                        _nextCuttoff = DateTime.UtcNow;
                    }
                }
            }

            if (!string.IsNullOrEmpty(SsasConnectionString))
            {
                var connectionStringBuilder = new SqlConnectionStringBuilder(ConnectionString);
                if (!string.IsNullOrEmpty(CreateDatabaseName))
                {
                    connectionStringBuilder.InitialCatalog = CreateDatabaseName;
                }
                else
                {
                    throw new Exception("Database must be specified either in the connection string or as the Database parameter");
                }
                _ssasExporter = new SsasExporter(SsasConnectionString, SsasDbName,
                    "Provider=SQLOLEDB;" + connectionStringBuilder.ConnectionString);
                _ssasExporter.Update = Update;
                _ssasExporter.Validate(tables, job);

                _ssasExporter.ReferenceDate = (_nextCuttoff ?? DateTime.UtcNow).Add(-staleTime);

                if (Rebuild)
                {
                    _ssasExporter.IncrementalUpdate = SqlClearOptions.None;
                }
                else
                {
                    _ssasExporter.IncrementalUpdate = SqlClearOptions;
                    if (hasPartitionKey)
                    {
                        _ssasExporter.IncrementalUpdate |= SqlClearOptions.Facts;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(SsasDbName))
            {
                throw new Exception("A connection string for SSAS Tabular is needed");
            }
        }

        public bool UpdateDataSource(IEnumerable<TableData> tables, IDataSource source)
        {
            if (Update)
            {
                //var staleTime = GetStaleTime(tables);
                source.ApplyUpdateFilter(_cutoff, null);
                return true;
            }
            else
            {
                if (source.Filters.Any(f => f.IsStagingFilter))
                {
                    //Rebuild on first update
                    _nextCuttoff = null;
                }
            }

            return false;
        }


        //TODO: Renew lock for long running tasks, and consistency check when reacquiring and releasing lock.
        private DateTime _lockDate;
        void AcquireLock(SqlConnection conn, SqlTransaction tran)
        {
            var cmd = new SqlCommand(@"DECLARE @LockDate datetime2;
                    UPDATE Sitecore.JobInfo SET @LockDate = [LockDate] = GetDate() WHERE [LockDate] IS NULL OR DateDiff(ss, [LockDate], GetDate()) > @LockTimeout;
                    SELECT @LockDate;", conn, tran);
            cmd.Parameters.AddWithValue("@LockTimeout", 60 * 30); //TODO: Make configurable

            var lockDate = cmd.ExecuteScalar();
            if (DBNull.Value.Equals(lockDate))
            {
                throw new InvalidOperationException("Another process is updating the database");
            }

            _lockDate = (DateTime)lockDate;
        }

        void ReleaseLock(SqlConnection conn, SqlTransaction tran)
        {
            var cmd = new SqlCommand(@"UPDATE Sitecore.JobInfo SET [LockDate] = NULL;", conn, tran);
            cmd.ExecuteNonQuery();
        }

        private void ValidateSchema(SqlConnection connection, IEnumerable<TableData> tables)
        {
            var schema = new SqlCommand("SELECT TOP 1 [Schema] FROM Sitecore.JobInfo", connection).ExecuteScalar() as string;
            if (schema == null ||
                !TableDataHelpers.FieldsAreEqual(tables.Select(t => t.Schema),
                    TableDataHelpers.Deserialize(schema)))
            {
                throw new Exception("Schema check failed. The existing schema does not match the job's schema.");
            }
        }

        private void UploadData(TableData table)
        {
            using (var subConn = new SqlConnection(ConnectionString))
            {
                subConn.Open();
                if (!string.IsNullOrEmpty(CreateDatabaseName))
                {
                    subConn.ChangeDatabase(CreateDatabaseName);
                }

                new SqlCommand(string.Format("TRUNCATE TABLE {0}", GetStagingTableName(table)), subConn)
                {
                    CommandTimeout = Timeout
                }.ExecuteNonQuery();

                using (var tran = subConn.BeginTransaction())
                {
                    using (var bcp = new SqlBulkCopy(subConn, SqlBulkCopyOptions.TableLock, tran))
                    {
                        bcp.BatchSize = 5000;
                        bcp.EnableStreaming = true;
                        bcp.BulkCopyTimeout = Timeout;
                        bcp.DestinationTableName = GetStagingTableName(table);
                        bcp.WriteToServer(table.CreateReader());
                    }
                    tran.Commit();
                }
            }
        }


        private void InsertOrUpdateData(TableData table, SqlConnection conn, SqlTransaction tran, string sourceTable = null)
        {
            var insert = false;
            if (Update)
            {
                var clear =
                    SqlClearOptions.HasFlag(table.Schema.IsDimension()
                        ? SqlClearOptions.Dimensions
                        : SqlClearOptions.Facts);

                if (!clear && _cutoff.HasValue)
                {
                    var pfield = SqlUpdateUtil.GetPartitionField(table.Schema);
                    if (pfield != null)
                    {
                        var sql = string.Format("DELETE {0} FROM {0} {1}", Escape(table.Name),
                            SqlUpdateUtil.GetUpdateCriteria(table.Schema, pfield, true, _cutoff));
                        new SqlCommand(sql, conn, tran)
                        {
                            CommandTimeout = Timeout
                        }.ExecuteNonQuery();
                        insert = true;
                    }
                }
                else
                {
                    new SqlCommand(string.Format("DELETE FROM {0}", Escape(table.Name)), conn, tran).ExecuteNonQuery();
                    insert = true;
                }
            }


            SqlCommand cmd;
            //Merge new dimension values
            if (Update && !insert)
            {
                var sql = new StringBuilder();

                sql.AppendFormat(@"MERGE [{0}] WITH (TABLOCK) AS Target USING {1} AS Source ON {2} ",
                    table.Name,
                    sourceTable ?? "@Data",
                    //Join criteria
                    string.Join(" AND ",
                        (table.Schema.Keys.Length > 0 ? table.Schema.Keys : table.Schema.Dimensions).Select(
                            field =>
                                string.Format("{0} = {1}", Escape("Target", field.Value.Name),
                                    Escape("Source", field.Value.Name)))));

                if (table.Schema.Fields.Any(f => !table.Schema.IsKey(f)))
                {
                    sql.AppendFormat(@"WHEN Matched THEN UPDATE SET {0}",
                        //Update fields
                    string.Join(", ",
                        table.Schema.Fields.Where(f => !table.Schema.IsKey(f)).Select(field =>
                            string.Format(
                                field.FieldType == FieldType.Fact ? "{0} = [Target].{0} + {1}" : "{0} = {1}", // <- Consider this. What if dimensions have measures?
                                Escape(field.Name), Escape("Source", field.Name)))));
                }


                sql.AppendFormat("WHEN NOT MATCHED THEN INSERT ({0}) VALUES ({1});",
                    //Insert fields                                                
                    string.Join(", ",
                        table.Schema.Fields.Select(field => Escape(field.Name))),
                    string.Join(", ",
                        table.Schema.Fields.Select(field => Escape("Source", field.Name)))
                    );

                cmd = new SqlCommand(sql.ToString(), conn, tran);
            }
            else
            {
                cmd =
                    new SqlCommand(
                        string.Format("INSERT {0} WITH (TABLOCK) SELECT * FROM {1}", Escape(table.Name), sourceTable ?? "@Data"), conn, tran);
            }

            cmd.CommandTimeout = Timeout;
            if (sourceTable == null)
            {
                var p = cmd.Parameters.AddWithValue("@Data", new SqlRecordAdapter(table));
                p.SqlDbType = SqlDbType.Structured;
                p.TypeName = GetTableTypeName(table);
            }
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (ArgumentException ex)
            {
                //Ignore. SqlCommand throws ArgumentException when table is empty.
            }
        }


        private static string Escape(params string[] path)
        {
            var i = 0;
            var s = new StringBuilder();
            foreach (var f in path)
            {
                if (i++ > 0) s.Append(".");
                s.AppendFormat("[{0}]", f);
            }
            return s.ToString();
        }

        private TimeSpan GetStaleTime(IEnumerable<TableData> tables)
        {
            var staleTime = TimeSpan.Zero;
            foreach (var table in tables)
            {
                if (table.Schema.IsCenterTable())
                {
                    var partitionField = SqlUpdateUtil.GetPartitionField(table.Schema);
                    if (partitionField != null && partitionField.Item2.StaleTime > staleTime)
                    {
                        staleTime = partitionField.Item2.StaleTime;
                    }
                }
            }

            return staleTime;
        }


        void WriteCreateTable(TextWriter writer, TableDataSchema table, string name = null, bool asType = false)
        {
            name = name ?? Escape(table.Name);

            var i = 0;
            if (asType)
            {
                writer.Write("CREATE TYPE {0} AS TABLE (", name);
            }
            else
            {
                writer.Write("CREATE TABLE {0} (", name);
            }

            foreach (var field in table.Fields)
            {
                writer.WriteLine(i++ > 0 ? "," : "");
                writer.Write("    [{0}] {1} {2}", field.Name,
                    GetColumnType(field.ValueType, field.FieldType == FieldType.Key ? 60 : (int?)null),
                    field.FieldType == FieldType.Key || (field.ValueType.IsValueType && Nullable.GetUnderlyingType(field.ValueType) == null) ? "NOT NULL" : "NULL");
            }

            if (table.Keys.Length > 0)
            {
                writer.WriteLine(i++ > 0 ? "," : "");
                //IGNORE_DUP_KEY to ignore hash collisions.
                writer.Write("    PRIMARY KEY CLUSTERED ({0}) WITH (IGNORE_DUP_KEY = ON)", string.Join(",", table.Keys.Select(pos => string.Format("[{0}]", pos.Value.Name))));
            }
            writer.WriteLine();

            writer.WriteLine(")");
            writer.WriteLine();
        }

        static string GetTableTypeName(TableData table)
        {
            return Escape(table.Name + "_Type");
        }

        static string GetStagingTableName(TableData table)
        {
            return Escape("Staging", table.Name + "_Staging");
        }

        void WriteSchema(TextWriter writer, IEnumerable<TableData> tables)
        {
            foreach (var table in tables)
            {
                WriteCreateTable(writer, table.Schema, GetTableTypeName(table), asType: true);
                WriteCreateTable(writer, table.Schema, GetStagingTableName(table));

                WriteCreateTable(writer, table.Schema);

                foreach (var p in table.Schema.Fields.OfType<PartitionField>())
                {
                    writer.WriteLine("CREATE INDEX {0} ON {1} ({2});", Escape(table.Name + "_" + p.Name), Escape(table.Name), Escape(p.Name));
                }
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

        protected virtual void OnBeforeCommit(SqlTransaction e)
        {
            EventHandler<SqlTransaction> handler = BeforeCommit;
            if (handler != null) handler(this, e);
        }

        protected virtual void OnSchemaCreating(SqlConnection e)
        {
            EventHandler<SqlConnection> handler = SchemaCreating;
            if (handler != null) handler(this, e);
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

        public void AdjustToUpdate()
        {
            Update = true;
        }

        public void AdjustToRebuild()
        {
            Rebuild = true;
            Update = true;
        }

        private static void DropAllTables(SqlConnection conn)
        {
            List<string> tablesToDrop = new List<string>();
            using (
                SqlDataReader sqlDataReader =
                    new SqlCommand(
                        "SELECT concat(TABLE_SCHEMA, '.', TABLE_NAME) FROM INFORMATION_SCHEMA.TABLES WHERE  TABLE_TYPE = 'BASE TABLE'",
                        conn).ExecuteReader())
            {
                while (sqlDataReader.Read())
                {
                    tablesToDrop.Add(sqlDataReader.GetString(0));
                }
            }

            foreach (string tableToDrop in tablesToDrop)
            {
                new SqlCommand($"DROP TABLE {tableToDrop};", conn).ExecuteNonQuery();
            }
        }

        private static void DropAllConstraints(SqlConnection conn)
        {
            List<KeyValuePair<string, string>> constraints = new List<KeyValuePair<string, string>>();
            using (
                SqlDataReader sqlDataReader =
                    new SqlCommand(
                        "SELECT CONCAT(OBJECT_SCHEMA_NAME(PARENT_OBJECT_ID), '.', OBJECT_NAME(PARENT_OBJECT_ID)), OBJECT_NAME(OBJECT_ID) FROM SYS.OBJECTS WHERE TYPE_DESC LIKE '%CONSTRAINT' AND NOT OBJECT_NAME(PARENT_OBJECT_ID) like 'TT_%' ORDER BY OBJECT_NAME(OBJECT_ID)",
                        conn).ExecuteReader())
            {
                while (sqlDataReader.Read())
                {
                    constraints.Add(new KeyValuePair<string, string>(sqlDataReader.GetString(0), sqlDataReader.GetString(1)));
                }
            }

            foreach (var constraint in constraints)
            {
                new SqlCommand($"ALTER TABLE {constraint.Key} DROP CONSTRAINT {constraint.Value};", conn).ExecuteNonQuery();
            }
        }

        private static void DropAllUserDefinedTypes(SqlConnection conn)
        {
            List<string> types = new List<string>();
            using (SqlDataReader sqlDataReader = new SqlCommand("SELECT name FROM SYS.types WHERE is_user_defined = 1", conn).ExecuteReader())
            {
                while (sqlDataReader.Read())
                {
                    types.Add(sqlDataReader.GetString(0));
                }
            }

            foreach (var type in types)
            {
                new SqlCommand($"DROP TYPE {type};", conn).ExecuteNonQuery();
            }
        }
    }
}