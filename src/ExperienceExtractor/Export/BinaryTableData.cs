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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;

namespace ExperienceExtractor.Export
{
    public class BinaryTableData : WritableTableData
    {
        public string Path { get; set; }

        public BinaryTableData(TableDataSchema schema, string path)
            : base(schema)
        {
            Path = path;
        }

        public override IEnumerable<object[]> Rows
        {
            get
            {
                if (!File.Exists(Path))
                {
                    yield break;
                }

                var readers = Schema.Fields.Select(f => GetReader(f.ValueType)).ToArray();
                
                using (var f = File.OpenRead(Path))
                using (var s = new GZipStream(f, CompressionMode.Decompress))
                using (var r = new BinaryReader(s))
                {                    
                    while (true)
                    {
                        var row = new object[Schema.Fields.Length];
                        try
                        {
                            for (var i = 0; i < readers.Length; i++)
                            {
                                row[i] = readers[i](r);
                            }
                        }
                        catch (EndOfStreamException)
                        {
                            break; //TODO: Better way to detect end of stream?
                        }
                        yield return row;
                    }
                }
            }
        }

        public override int? RowCount
        {
            get { return null; }
        }

        public override void Dispose()
        {

        }

        public override void WriteRows(IEnumerable<object[]> rows)
        {
            Directory.CreateDirectory(new FileInfo(Path).DirectoryName);

            var writers = Schema.Fields.Select(f => GetWriter(f.ValueType)).ToArray();

            using (var f = File.OpenWrite(Path))
            using (var s = new GZipStream(f, CompressionLevel.Fastest))
            using (var w = new BinaryWriter(s))
            {                
                foreach (var row in rows)
                {
                    for (var i = 0; i < writers.Length; i++)
                    {
                        writers[i](w, row[i]);
                    }
                }
            }
        }

        static Action<BinaryWriter, object> GetWriter(Type t)
        {
            var inner = Nullable.GetUnderlyingType(t);
            if (inner != null)
            {
                var valueFormatter = GetWriter(inner);
                return (w, o) =>
                {
                    w.Write(o != null);
                    if (o != null)
                    {
                        valueFormatter(w, o);
                    }
                };
            }

            if (t == typeof(string))
            {
                return (w, o) => w.Write((string)o ?? "");
            }
            if (t == typeof(int))
            {
                return (w, o) => w.Write((int)o);
            }
            if (t == typeof(long))
            {
                return (w, o) => w.Write((long)o);
            }
            if (t == typeof(Guid))
            {
                return (w, o) => w.Write(((Guid)o).ToByteArray());
            }
            if (t == typeof(DateTime))
            {
                return (w, o) => w.Write(((DateTime)o).Ticks);
            }
            if (t == typeof(TimeSpan))
            {
                return (w, o) => w.Write(((TimeSpan)o).Ticks);
            }

            throw new NotSupportedException(string.Format("Unsupported data type: {0}", t.FullName));
        }


        static Func<BinaryReader, object> GetReader(Type t)
        {
            var inner = Nullable.GetUnderlyingType(t);
            if (inner != null)
            {
                var valueFormatter = GetReader(inner);
                return r => r.ReadBoolean() ? valueFormatter(r) : null;
            }

            if (t == typeof(string))
            {
                return r => r.ReadString();
            }
            if (t == typeof(int))
            {
                return r => r.ReadInt32();
            }
            if (t == typeof(long))
            {
                return r => r.ReadInt64();
            }
            if (t == typeof(Guid))
            {                
                return r =>
                {
                    var bytes = r.ReadBytes(16);
                    if( bytes.Length < 16) throw new EndOfStreamException();
                    return new Guid(bytes);
                };
            }
            if (t == typeof(DateTime))
            {
                return r => new DateTime(r.ReadInt64());
            }
            if (t == typeof(TimeSpan))
            {
                return r => new TimeSpan(r.ReadInt64());
            }

            throw new NotSupportedException(string.Format("Unsupported data type: {0}", t.FullName));
        }        
    }
}
