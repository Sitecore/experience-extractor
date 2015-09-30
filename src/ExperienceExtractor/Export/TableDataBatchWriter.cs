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
using System.Threading;
using ExperienceExtractor.Data;

namespace ExperienceExtractor.Export
{
    public class TableDataBatchWriter : ITableDataBatchWriter
    {
        public ITablePartitionSource PartitionSource { get; set; }

        public object SyncLock { get; set; }

        public long? MaximumSize { get; set; }

        public TableDataBatchWriter(ITablePartitionSource partitionSource)
        {
            PartitionSource = partitionSource;
        }

        private readonly List<TablePartition> _partitions = new List<TablePartition>();

        public long Size { get; private set; }

        public void WriteBatch(IEnumerable<TableData> tables)
        {
            if( SyncLock != null) Monitor.Enter(SyncLock);
            try
            {
                var partition = PartitionSource.CreatePartition();
                _partitions.Add(partition);

                foreach (var table in tables)
                {
                    using (var writer = partition.CreateTableDataWriter(table.Schema))
                    {
                        writer.WriteRows(table.Rows);
                    }
                }
                Size += partition.Size;
            }
            finally
            {
                if( SyncLock != null) Monitor.Exit(SyncLock);
            }
        }

        public IEnumerable<TableData> Tables
        {
            get
            {
                return MergedTableData.FromTableSets(_partitions.Select(p => p.Tables));
            }
        }

        void DisposePartitions()
        {
            foreach (var partition in _partitions)
            {
                partition.Dispose();
            }
            _partitions.Clear();
        }

        public void Dispose()
        {
            DisposePartitions();
            var d = PartitionSource as IDisposable;
            if (d != null)
            {
                d.Dispose();
            }
        }

        public bool End
        {
            get { return MaximumSize.HasValue && Size > MaximumSize; }
        }
    }
}
