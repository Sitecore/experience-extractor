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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Export
{
    public class CsvTableData : WritableTableData
    {
        public string Path { get; set; }
        public string Delimiter { get; set; }
        public CultureInfo NumberCulture { get; set; }

        public string DateFormat { get; set; }
        public string DateTimeFormat { get; set; }

        public CsvTableData(TableDataSchema schema, string path, string delimiter = "\t", CultureInfo numberCulture = null)
            : base(schema)
        {
            Path = path;
            Delimiter = delimiter;
            NumberCulture = numberCulture ?? CultureInfo.CurrentCulture;
            DateFormat = "yyyy-MM-dd";
            DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        }


        public override int? RowCount
        {
            get { return null; }
        }


        public override IEnumerable<object[]> Rows
        {
            get
            {
                if (!File.Exists(Path))
                {
                    yield break;
                }

                using (var f = new StreamReader(File.Open(Path, FileMode.Open), Encoding.UTF8, true))
                using (
                    var csv = new CsvReader(f,
                        new CsvConfiguration { Delimiter = Delimiter, CultureInfo = NumberCulture, HasHeaderRecord = true }))
                {
                    var converters = Schema.Fields.Select(field => field.ValueType)
                            .Select(p => Nullable.GetUnderlyingType(p) ?? p)
                            .Select(TypeDescriptor.GetConverter)
                            .ToArray();

                    while (csv.Read())
                    {
                        var row = new object[Schema.Fields.Length];
                        for (var i = 0; i < Schema.Fields.Length; i++)
                        {
                            row[i] = string.IsNullOrEmpty(csv.CurrentRecord[i])
                                ? null
                                : converters[i].ConvertFromString(null, NumberCulture, csv.CurrentRecord[i]);
                        }

                        yield return row;
                    }
                }
            }
        }

        public override void Dispose()
        {            
        }

        public override void WriteRows(IEnumerable<object[]> rows)
        {
            Directory.CreateDirectory(new FileInfo(Path).DirectoryName);

            var n = 0;
            using (var writer = File.CreateText(Path))
            using (
                var csv = new CsvWriter(writer,
                    new CsvConfiguration { Delimiter = Delimiter, CultureInfo = NumberCulture }))
            {
                WriteHeader(csv);
                foreach (var row in rows)
                {
                    var i = 0;
                    foreach (var field in Schema.Fields)
                    {
                        var v = row[i];
                        if (v != null && DateFormat != null &&
                            (field.ValueType == typeof(DateTime) || field.ValueType == typeof(DateTime?)))
                        {
                            var d = (DateTime) v;
                            csv.WriteField(d.ToString(d == d.Date ? DateFormat : DateTimeFormat, CultureInfo.InvariantCulture));
                        }                        
                        else
                        {
                            csv.WriteField(v);
                        }
                        ++i;
                    }
                    ++n;
                    csv.NextRecord();
                }
            }
        }

        void WriteHeader(CsvWriter csv)
        {
            foreach (var field in Schema.Fields)
            {
                csv.WriteField(field.Name);
            }
            csv.NextRecord();
        }

        //public static IList<TableData> Load  
    }
}
