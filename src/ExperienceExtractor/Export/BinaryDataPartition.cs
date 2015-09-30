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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Data.Schema;

namespace ExperienceExtractor.Export
{
    public class BinaryDataPartition : TablePartition
    {
        public BinaryDataPartition(string directory)
        {     
            Directory = directory;
        }

        public string Directory { get; set; }

        public override long Size
        {
            get
            {
                var dir = new DirectoryInfo(Directory);
                return dir.Exists ? dir.GetFiles().Sum(f => f.Length) : 0;
            }
        }

        public override ITableDataWriter CreateTableDataWriter(TableDataSchema schema)
        {
            var data = new BinaryTableData(schema, Path.Combine(Directory, schema.Name + ".bin"));
            AddTableData(data);
            return data;
        }

        public override void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, true);
            }
        }
    }
}
