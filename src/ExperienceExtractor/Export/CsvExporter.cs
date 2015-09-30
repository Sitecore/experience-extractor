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
using System.ComponentModel;
using System.Data.Linq;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;
using Sitecore.Pipelines.RenderLayout;
using DateTime = System.DateTime;
using File = System.IO.File;


namespace ExperienceExtractor.Export
{
    /// <summary>
    /// Persists table data as CSV files
    /// </summary>
    public class CsvExporter : ITableDataExporter, ITablePartitionSource
    {

        public string Directory { get; set; }
        public string Delimiter { get; set; }
        public bool BinaryPartitions { get; set; }
        public bool BinaryOutput { get; set; }
        public bool KeepOutput { get; set; }
        public string PartitionPrefix { get; set; }

        private readonly CultureInfo _numberCulture;

        public CsvExporter(string directory, string delimiter = "\t", CultureInfo numberCulture = null, bool binaryPartitions = true, bool binaryOutput = false, bool keepOutput = true)
        {
            Directory = directory;
            Delimiter = delimiter;
            BinaryPartitions = binaryPartitions;
            BinaryOutput = binaryOutput;
            KeepOutput = keepOutput;
            PartitionPrefix = "~";

            _numberCulture = numberCulture ?? CultureInfo.GetCultureInfo("en-US");
        }

        public IEnumerable<TableData> Export(IEnumerable<TableData> tables)
        {
            if (!System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }

            var csvTables = new List<WritableTableData>();
            foreach (var table in tables)
            {
                var writer = CreateTableData(table.Schema,
                    Path.Combine(Directory, table.Name + ".txt"));

                writer.WriteRows(table.Rows);
                csvTables.Add(writer);
            }
            WriteSchema(Path.Combine(Directory, "schema.ini"), tables.Select(t => t.Schema));

            return csvTables;
        }

        private WritableTableData CreateTableData(TableDataSchema schema, string path)
        {
            if (BinaryOutput)
            {
                return new BinaryTableData(schema, path.Replace(".txt", ".bin"));
            }
            return new CsvTableData(schema, path,
                Delimiter, _numberCulture);
        }

        private static readonly Dictionary<Type, string> CsvTypes = new Dictionary<Type, string>
        {
            {typeof(bool), "Bit"},
            {typeof(byte), "Byte"},
            {typeof(short), "Short"},
            {typeof(int), "Long"},
            {typeof(long), "Decimal"},
            {typeof(decimal), "Currency"},
            {typeof(float), "Single"},
            {typeof(double), "Double"},
            {typeof(DateTime), "DateTime"},
            {typeof(string), "Text"},
            {typeof(Guid), "Text Width 36"}
        };


        void WriteSchema(string path, IEnumerable<TableDataSchema> tables)
        {
            using (var f = new StreamWriter(File.Open(path, FileMode.Create), Encoding.GetEncoding(1252)))
                foreach (var table in tables)
                {
                    f.WriteLine("[{0}.txt]", table.Name.ToLower());
                    f.WriteLine("ColNameHeader=True");
                    f.WriteLine("Format=TabDelimited");
                    f.WriteLine("CharacterSet=65001");
                    f.WriteLine("DateTimeFormat=yyyy-MM-dd");
                    f.WriteLine("DecimalSymbol=.");
                    f.WriteLine("MaxScanRows=2");
                    var pos = 1;
                    foreach (var field in table.Fields)
                    {
                        var t = Nullable.GetUnderlyingType(field.ValueType) ?? field.ValueType;
                        string typeName;
                        typeName = CsvTypes.TryGetValue(t, out typeName) ? typeName : "Text";
                        f.WriteLine(@"Col{0}=""{1}"" {2}", pos++, field.Name, typeName);
                    }
                }

        }

        private int _nextPartition;
        public TablePartition CreatePartition()
        {
            return BinaryPartitions
                ? new BinaryDataPartition(Path.Combine(Directory, PartitionPrefix + _nextPartition++))
                : (TablePartition)new CsvPartition(Path.Combine(Directory, PartitionPrefix + _nextPartition++), this);
        }

        class CsvPartition : TablePartition
        {
            private readonly CsvExporter _owner;
            public string Directory { get; set; }

            public CsvPartition(string directory, CsvExporter owner)
            {
                _owner = owner;
                Directory = directory;
            }

            public override long Size
            {
                get
                {
                    var dir = new DirectoryInfo(Directory);
                    return dir.Exists ? dir.GetFiles().Sum(f => f.Length) : 0;
                }
            }

            public override ITableDataWriter CreateTableDataWriter(TableDataSchema table)
            {
                var csvTable = _owner.CreateTableData(table, Path.Combine(Directory, table.Name + ".txt"));
                AddTableData(csvTable);
                return csvTable;
            }

            public override void Dispose()
            {
                if (System.IO.Directory.Exists(Directory))
                {
                    System.IO.Directory.Delete(Directory, true);
                }
            }
        }

        public void Dispose()
        {
            if (!KeepOutput && System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, true);
            }
        }
    }
}