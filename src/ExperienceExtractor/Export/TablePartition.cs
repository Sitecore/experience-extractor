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
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Export
{
    /// <summary>
    /// A partition of data offloaded to disk during processing in a <see cref="DataProcessor"/>    
    /// </summary>
    public abstract class TablePartition : IDisposable
    {
        private readonly List<TableData> _tables = new List<TableData>();

        /// <summary>
        /// The tables in this partition
        /// </summary>
        public IEnumerable<TableData> Tables { get { return _tables; }}

        /// <summary>
        /// The byte size of this partition
        /// </summary>
        public abstract long Size { get; }

        /// <summary>
        /// Adds a table with the specified schema to this partition and returns a <see cref="ITableDataWriter"/> for writing data to it
        /// </summary>
        /// <param name="schema">The schema to add a table for</param>
        /// <returns></returns>
        public abstract ITableDataWriter CreateTableDataWriter(TableDataSchema schema);                

        /// <summary>
        /// Adds a table to this partition
        /// </summary>
        /// <param name="data"></param>
        protected void AddTableData(TableData data)
        {
            _tables.Add(data);
        }

        /// <summary>
        /// Frees resources used by this partition after it has been merged with others.
        /// </summary>
        public abstract void Dispose();
    }
}