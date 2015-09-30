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
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using Microsoft.SqlServer.Server;
using Sitecore.Analytics.Aggregation.Data.Model;
using SortOrder = ExperienceExtractor.Data.Schema.SortOrder;

namespace ExperienceExtractor.Components.PostProcessors
{
    public class SqlRecordAdapter : IEnumerable<SqlDataRecord>
    {
        private readonly TableData _data;

        public SqlRecordAdapter(TableData data)
        {
            _data = data;
        }

        public IEnumerator<SqlDataRecord> GetEnumerator()
        {

            var sortIndices = Enumerable.Repeat(-1, _data.Schema.Fields.Length).ToArray();
            var ix = 0;
            foreach (var field in RowComparer.GetSortFields(_data.Schema))
            {
                sortIndices[field.Index] = ix++;
            }


            var metadata = _data.Schema.Fields.Select((f,i) =>
            {
                var t = _types[f.ValueType];

                var sortOrder = sortIndices[i] != -1
                    ? (f.SortOrder == SortOrder.Descending
                        ? System.Data.SqlClient.SortOrder.Descending
                        : System.Data.SqlClient.SortOrder.Ascending)
                    : System.Data.SqlClient.SortOrder.Unspecified;

                if (t == SqlDbType.NVarChar || t == SqlDbType.VarChar || t == SqlDbType.VarBinary)
                {
                    return new SqlMetaData(f.Name, t, SqlMetaData.Max, false, _data.Schema.IsKey(f), sortOrder, sortIndices[i]);
                }

                return new SqlMetaData(f.Name, t, false, _data.Schema.IsKey(f), sortOrder, sortIndices[i]);
            }).ToArray();

            var record = new SqlDataRecord(metadata);
            foreach (var row in _data.Rows)
            {
                for (var i = 0; i < row.Length; i++)
                {
                    record.SetValue(i, row[i]);
                }
                yield return record;
            }            
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }



        static Dictionary<Type, SqlDbType> _types = new Dictionary<Type, SqlDbType>()
        {
            {typeof(long), SqlDbType.BigInt},
            {typeof(long?), SqlDbType.BigInt},
            {typeof(int), SqlDbType.Int},
            {typeof(int?), SqlDbType.Int},
            {typeof(byte), SqlDbType.TinyInt},
            {typeof(byte?), SqlDbType.TinyInt},
            {typeof(DateTime), SqlDbType.DateTime},
            {typeof(DateTime?), SqlDbType.DateTime},
            {typeof(bool), SqlDbType.Bit},
            {typeof(bool?), SqlDbType.Bit},
            {typeof(decimal), SqlDbType.Decimal},
            {typeof(decimal?), SqlDbType.Decimal},
            {typeof(double), SqlDbType.Float},
            {typeof(double?), SqlDbType.Float},
            {typeof(Guid), SqlDbType.UniqueIdentifier},
            {typeof(Guid?), SqlDbType.UniqueIdentifier},
            {typeof(string), SqlDbType.NVarChar}
        };


    }
}
