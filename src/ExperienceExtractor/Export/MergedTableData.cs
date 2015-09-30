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

using System.Collections.Generic;
using System.Linq;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.Helpers;

namespace ExperienceExtractor.Export
{
    public class MergedTableData : TableData
    {
        public MergedTableData(TableDataSchema schema, IEnumerable<TableData> sources = null)
            : base(schema)
        {
            Sources = sources != null ? sources.ToList() : new List<TableData>();
        }

        public List<TableData> Sources { get; private set; }

        public override IEnumerable<object[]> Rows
        {
            get
            {
                if (Sources.Count == 0) return Enumerable.Empty<object[]>();

                if (Sources.Count == 1)
                {
                    return Sources[0].Rows;
                }
                
                var comparer = new RowComparer(Schema);

                if (Schema.Name == "MonthlyVisits")
                {
                    var b = 4;
                }
                //O(Log(M)*N) compared to the old O(M*N)
                return Sources.Select(t => t.Rows).ToList().MergeSorted(MergeFacts, comparer);

                
                //IEnumerable<object[]> rows = null;
                //foreach (var source in Sources)
                //{
                //    rows = rows == null ? source.Rows : rows.MergeSorted(source.Rows, comparer);
                //}

                //return rows.MergeDupplicates(MergeFacts, comparer);
            }
        }

        public override int? RowCount
        {
            get { return null; }
        }


        public static IEnumerable<TableData> FromTableSets(IEnumerable<IEnumerable<TableData>> datasets)
        {
            var tables = new Dictionary<string, MergedTableData>();

            foreach (var partition in datasets)
            {
                foreach (var table in partition)
                {
                    MergedTableData current;
                    if (!tables.TryGetValue(table.Name, out current))
                    {
                        tables.Add(table.Name, current = new MergedTableData(table.Schema));
                    }
                    current.Sources.Add(table);
                }
            }

            return tables.Values;
        }
    }

}