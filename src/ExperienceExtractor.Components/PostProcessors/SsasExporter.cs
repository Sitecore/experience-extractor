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
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;
using Microsoft.AnalysisServices;
using MicrosoftSql2012Samples.Amo2Tabular;

namespace ExperienceExtractor.Components.PostProcessors
{
    public class SsasExporter : ITableDataPostProcessor
    {
        public string ConnectionString { get; set; }
        public string DbName { get; set; }
        public string SourceConnectionString { get; set; }
        public bool Update { get; set; }

        public SqlClearOptions IncrementalUpdate { get; set; }

        public DateTime? CutOff { get; set; }

        public DateTime? ReferenceDate { get; set; }

        public SsasExporter(string connectionString, string dbName, string sourceConnectionString)
        {
            ConnectionString = connectionString;
            DbName = dbName;
            SourceConnectionString = sourceConnectionString;
            IncrementalUpdate = SqlClearOptions.Facts;
        }

        public string Name { get { return "SSAS Tabular"; } }

        public void Process(string tempDirectory, IEnumerable<TableData> sourceTables, IJobSpecification job)
        {
            var tables = sourceTables.Select(t => t.Schema).ToArray();

            using (var server = new Server())
            {
                server.Connect(ConnectionString);

                var db = server.Databases.Find(DbName);

                if (!Update)
                {
                    if (db != null)
                    {
                        db.Drop();
                    }

                    CreateSchema(db, server, tables);
                    db = server.Databases.Find(DbName);
                }

                //server.CaptureXml = true; <- Doesn't work with QueryBinding and errors from empty partitions marked as never processed.

                foreach (var table in sourceTables)
                {
                    
                    var processingType = !Update && IncrementalUpdate.HasFlag(
                        table.Schema.IsDimension() ? SqlClearOptions.Dimensions : SqlClearOptions.Facts)
                        ? ProcessType.ProcessAdd
                        : ProcessType.ProcessFull;

                    if (table.Schema.IsDimension())
                    {
                        ProcessPartition(db, table.Name, table.Name, processingType);
                    }
                    else
                    {
                        var partition = SqlUpdateUtil.GetPartitionField(table.Schema);
                        if (partition != null)
                        {
                            ProcessPartition(db, table.Name, GetTransientPartitionName(table.Schema), ProcessType.ProcessFull,
                                string.Format("SELECT [{0}].* FROM [{0}] {1}", table.Name, SqlUpdateUtil.GetUpdateCriteria(table.Schema, partition, true, date: ReferenceDate)));

                            ProcessPartition(db, table.Name, table.Name, processingType,
                                string.Format("SELECT [{0}].* FROM [{0}] {1}", table.Name, SqlUpdateUtil.GetUpdateCriteria(table.Schema, partition, false, date: ReferenceDate, cutoff: CutOff)));
                        }
                        else
                        {
                            ProcessPartition(db, table.Name, table.Name, processingType);
                        }
                    }
                }

                //server.ExecuteCaptureLog(true, true);
            }
        }

        static void ProcessPartition(Database db, string tableName, string partitionName, ProcessType type, string sql = null)
        {
            var id = db.Dimensions.GetByName(tableName).ID;
            using (var measureGroup = db.Cubes[0].MeasureGroups[id])
            {
                using (var p = measureGroup.Partitions.GetByName(partitionName))
                {
                    try
                    {
                        if (sql == null)
                        {
                            p.Process(type);
                        }
                        else
                        {
                            p.Process(type, new QueryBinding(db.DataSourceViews[0].ID, sql));
                        }
                    }
                    catch (OperationException oe)
                    {
                        //Partition is empty
                        //TODO: Find better way to check if this is the cause of the exception. HResult just says "Unspecified error".
                        if (type == ProcessType.ProcessAdd)
                        {
                            ProcessPartition(db, tableName, partitionName, ProcessType.ProcessFull, sql);
                        }
                        else
                        {
                            throw oe;
                        }
                    }

                }
            }
        }

        private void CreateSchema(Database db, Server server, TableDataSchema[] tables)
        {
            using (db = AMO2Tabular.TabularDatabaseAdd(server,
                DbName,
                SourceConnectionString,
                "Sql Server"))
            {
                var i = 0;
                foreach (var table in tables)
                {
                    if (i++ == 0)
                    {
                        AMO2Tabular.TableAddFirstTable(db, "Model", table.Name, table.Name);
                    }
                    else
                    {
                        AMO2Tabular.TableAdd(db, table.Name, table.Name);
                    }

                    foreach (var field in table.Fields)
                    {
                        if (!string.IsNullOrEmpty(field.FriendlyName))
                        {
                            AMO2Tabular.ColumnAlterColumnName(db, table.Name, field.Name, field.FriendlyName, false);
                        }

                        if (field is PartitionField)
                        {
                            AMO2Tabular.ColumnDrop(db, table.Name, field.Name, false);
                        }
                        else if (field.FieldType == FieldType.Fact)
                        {
                            var measureName = PostFixMeasureName(table, string.Format("Total {0}", field.Name));
                            AMO2Tabular.MeasureAdd(db, table.Name, measureName,
                                "SUM([" + GetFieldName(table, field.Name) + "])", updateInstance: false);

                            var isInteger = field.ValueType == typeof(int) || field.ValueType == typeof(long);
                            SetMeasureFormat(db, measureName,
                                isInteger ? CalculatedFieldFormat.Integer : CalculatedFieldFormat.Decimal);
                        }
                        else if (!string.IsNullOrEmpty(field.SortBy))
                        {
                            AMO2Tabular.ColumnAlterSortByColumnName(db, table.Name, GetFieldFriendlyName(table, field.Name), GetFieldFriendlyName(table, field.SortBy),
                                updateInstance: false);
                        }
                        else if (field.Hide)
                        {

                            AMO2Tabular.ColumnAlterVisibility(db, table.Name, GetFieldName(table, field.Name), false, false);
                        }
                    }

                    if (SqlUpdateUtil.GetPartitionField(table) != null)
                    {
                        AMO2Tabular.PartitionAdd(db, table.Name, GetTransientPartitionName(table), string.Format("SELECT * FROM [{0}] WHERE 1=0", table.Name), false);
                    }

                    if (table.TableType == "Date")
                    {
                        //AMO2Tabular doesn't make the field the time dimension's key. This code does that.
                        var dateField = table.Fields.First(f => f.ValueType == typeof(DateTime) && !f.Hide);
                        var dim = db.Dimensions.GetByName(table.Name);
                        dim.Type = DimensionType.Time;
                        var attr = db.Dimensions.GetByName(table.Name).Attributes.GetByName(GetFieldName(table, dateField.Name));
                        attr.Usage = AttributeUsage.Key;
                        attr.FormatString = "General Date";
                        var rowNumber =
                            dim.Attributes.Cast<DimensionAttribute>().First(a => a.Type == AttributeType.RowNumber);
                        rowNumber.Usage = AttributeUsage.Regular;
                        rowNumber.AttributeRelationships.Remove(attr.ID);

                        var rel = attr.AttributeRelationships.Add(rowNumber.ID);
                        rel.Cardinality = Cardinality.One;
                        attr.KeyColumns[0].NullProcessing = NullProcessing.Error;
                        attr.KeyUniquenessGuarantee = true;

                        ((RegularMeasureGroupDimension)db.Cubes[0].MeasureGroups[dim.ID].Dimensions[dim.ID]).Attributes
                            [attr.ID].KeyColumns[0].NullProcessing = NullProcessing.Error;
                        //attr.AttributeRelationships
                    }

                }


                foreach (var table in tables)
                {
                    foreach (var relation in table.RelatedTables)
                    {
                        if (relation.RelationType == RelationType.Dimension ||
                            relation.RelationType == RelationType.Parent)
                        {
                            AMO2Tabular.RelationshipAdd(db, relation.RelatedTable.Name,
                                relation.RelatedFields.First().Name, table.Name, relation.Fields.First().Name,
                                updateInstance: false);
                        }
                    }

                    foreach (var calculatedField in table.CalculatedFields)
                    {
                        var dax = CalculatedField.FormatDax(calculatedField.DaxPattern, table);
                        if (!string.IsNullOrEmpty(dax))
                        {
                            var measureName = PostFixMeasureName(table, calculatedField.Name);
                            AMO2Tabular.MeasureAdd(db, table.Name, measureName, dax, updateInstance: false);


                            if (!string.IsNullOrEmpty(calculatedField.FormatString))
                            {
                                SetMeasureFormat(db, measureName, calculatedField.FormatString);
                            }
                        }

                        if (!string.IsNullOrEmpty(calculatedField.ChildDaxPattern))
                        {
                            //TODO: Deeper nested tables
                            foreach (var rel in table.RelatedTables.Where(r => r.RelationType == RelationType.Child))
                            {
                                var childDax = CalculatedField.FormatDax(calculatedField.ChildDaxPattern, rel.RelatedTable);
                                if (!string.IsNullOrEmpty(childDax))
                                {
                                    var measureName = PostFixMeasureName(rel.RelatedTable, calculatedField.Name);
                                    AMO2Tabular.MeasureAdd(db, rel.RelatedTable.Name, measureName, childDax,
                                        updateInstance: false);

                                    if (!string.IsNullOrEmpty(calculatedField.FormatString))
                                    {
                                        SetMeasureFormat(db, measureName, calculatedField.FormatString);
                                    }
                                }
                            }
                        }
                    }
                    db.Update(UpdateOptions.ExpandFull, UpdateMode.Default);
                }

            }
        }

        string GetTransientPartitionName(TableDataSchema schema)
        {
            return schema.Name + "_Transient";
        }

        static void SetMeasureFormat(Database db, string measureName, string formatString)
        {
            var script = db.Cubes[0].MdxScripts["MdxScript"];
            var cp = script.CalculationProperties.Find(measureName);
            script.CalculationProperties.Remove(cp);
            cp.CalculationReference = "[" + measureName + "]";
            cp.FormatString = formatString;
            script.CalculationProperties.Add(cp);
        }

        static string GetFieldName(TableDataSchema table, string fieldName)
        {
            var field = table.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (field == null)
            {
                throw new KeyNotFoundException(string.Format("No field with name '{0}' in table '{1}", fieldName, table.Name));
            }

            return string.IsNullOrEmpty(field.FriendlyName) ? field.Name : field.FriendlyName;
        }

        static string GetFieldFriendlyName(TableDataSchema table, string fieldName)
        {
            return GetFieldName(table, fieldName);
        }


        static string PostFixMeasureName(TableDataSchema table, string name)
        {
            var hasParent = table.RelatedTables.Any(r => r.RelationType == RelationType.Parent);

            return hasParent ? name + " (" + table.Name + ")" : name;
        }

        public void Validate(IEnumerable<TableData> tables, IJobSpecification job)
        {
            using (var server = new Server())
            {
                server.Connect(ConnectionString);
            }
        }

        public bool UpdateDataSource(IEnumerable<TableData> tables, IDataSource source)
        {
            throw new InvalidOperationException("This data source is not supposed to be invoked directly.");
        }
    }
}
