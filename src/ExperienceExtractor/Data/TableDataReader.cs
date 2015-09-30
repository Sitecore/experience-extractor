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
using System.Data.Common;
using System.Linq;

namespace ExperienceExtractor.Data
{
    internal class TableDataReader : DbDataReader
    {
        
        private readonly IEnumerator<object[]> _source;
        private object[] _current;
        private readonly FieldDefinition[] _fields;
        private readonly string[] _names;

        public TableDataReader(TableData tableData)
        {            
            _source = tableData.Rows.GetEnumerator();
            _fields = tableData.Schema.Fields.Select(f =>
                new FieldDefinition
                {
                    AllowNull = f.ValueType.IsValueType && Nullable.GetUnderlyingType(f.ValueType) == null,
                    Name = f.Name,
                    Type = Nullable.GetUnderlyingType(f.ValueType) ?? f.ValueType
                }
             ).ToArray();
            _names = _fields.Select(f => f.Name).ToArray();
        }


        public override void Close()
        {
            Dispose();
        }

        public override int Depth
        {
            get { return 0; }
        }

        public override DataTable GetSchemaTable()
        {
            // these are the columns used by DataTable load
            var table = new DataTable
            {
                Columns =
                    {
                        {"ColumnOrdinal", typeof (int)},
                        {"ColumnName", typeof (string)},
                        {"DataType", typeof (Type)},
                        {"ColumnSize", typeof (int)},
                        {"AllowDBNull", typeof (bool)}
                    }
            };
            var rowData = new object[5];
            for (int i = 0; i < _fields.Length; i++)
            {
                rowData[0] = i;
                rowData[1] = _fields[i].Name;
                rowData[2] = _fields[i].Type;
                rowData[3] = -1;
                rowData[4] = _fields[i].AllowNull;
                table.Rows.Add(rowData);
            }
            return table;
        }
        
        public override bool HasRows
        {
            get { return _current != null; }
        }

        public override bool IsClosed
        {
            get { return _source == null; }
        }

        public override bool NextResult()
        {
            return false;
        }

        public override bool Read()
        {
            if (_source != null && _source.MoveNext())
            {                
                _current = _source.Current;
                return true;
            }
            _current = null;
            return false;
        }

        public override int RecordsAffected
        {
            get { return 0; }
        }


        public void Dispose()
        {
        }

        public override int FieldCount
        {
            get { return _fields.Length; }
        }

        public override bool GetBoolean(int i)
        {
            return (bool)this[i];
        }

        public override byte GetByte(int i)
        {
            return (byte)this[i];
        }

        public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            var s = (byte[])this[i];
            var available = s.Length - (int)fieldOffset;
            if (available <= 0) return 0;

            var count = Math.Min(length, available);
            Buffer.BlockCopy(s, (int)fieldOffset, buffer, bufferoffset, count);
            return count;
        }

        public override char GetChar(int i)
        {
            return (char)this[i];
        }

        public override long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            var s = (string)this[i];
            var available = s.Length - (int)fieldoffset;
            if (available <= 0) return 0;

            var count = Math.Min(length, available);
            s.CopyTo((int)fieldoffset, buffer, bufferoffset, count);
            return count;
        }

        //IDataReader IDataRecord.GetData(int i)
        //{
        //    throw new NotSupportedException();
        //}

        public override string GetDataTypeName(int i)
        {
            return _fields[i].Name;
        }

        public override IEnumerator GetEnumerator()
        {
            return new DbEnumerator(this, true);
        }

        public override System.DateTime GetDateTime(int i)
        {
            return (System.DateTime)this[i];
        }

        public override decimal GetDecimal(int i)
        {
            return (decimal)this[i];
        }

        public override double GetDouble(int i)
        {
            return (double)this[i];
        }

        public override Type GetFieldType(int i)
        {
            return _fields[i].Type;
        }

        public override float GetFloat(int i)
        {
            return (float)this[i];
        }

        public override Guid GetGuid(int i)
        {
            return (Guid)this[i];
        }

        public override short GetInt16(int i)
        {
            return (short)this[i];
        }

        public override int GetInt32(int i)
        {
            return (int)this[i];
        }

        public override long GetInt64(int i)
        {
            return (long)this[i];
        }

        public override string GetName(int i)
        {
            return _fields[i].Name;
        }

        public override int GetOrdinal(string name)
        {
            return Array.IndexOf(_names, name);
        }

        public override string GetString(int i)
        {
            return (string)this[i];
        }

        public override object GetValue(int i)
        {
            return this[i];
        }

        public override int GetValues(object[] values)
        {
            var size = values.Length < _current.Length ? values.Length : _current.Length;
            Array.Copy(_current, values, size);
            for (var i = 0; i < size; i++)
            {
                values[i] = values[i] ?? DBNull.Value;
            }
            return size;
        }

        public override bool IsDBNull(int i)
        {
            return this[i] is DBNull || this[i] == null;
        }

        public override object this[string name]
        {
            get { return _current[GetOrdinal(name)] ?? DBNull.Value; }

        }
        
        public override object this[int i]
        {
            get { return _current[i] ?? DBNull.Value; }
        }

        struct FieldDefinition
        {
            public string Name;
            public bool AllowNull;
            public Type Type;
        }
    }
}
