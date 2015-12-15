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
using System.Linq;
using ExperienceExtractor.Data;
using ExperienceExtractor.Export;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Processing
{

    /// <summary>
    /// Provides the context for data extractions.
    /// A single root <see cref="ITableMapper"/> defines all the tables and fields that are extracted
    /// The <see cref="ITableMapper"/> is initialized by the data processor, and the data processor provides context where the different mappers' <see cref="TableData"/>s are collected.   
    /// A data processor is responsible for constraining the amount of data stored in memory by offloading data to a <see cref="ITableDataBatchWriter"/>.
    /// The combined table data result from the batch writer and data stored in memory is accessed through the data processor.
    /// </summary>
    public class DataProcessor
    {

        public TableMap TableMap { get; private set; }

        public ITableDataBatchWriter BatchWriter { get; set; }

        public ITableMapper TableMapper { get; set; }
        public int BatchSize { get; set; }
        
        public int RowsCreated
        {
            get { return TableMap.Tables.Cast<TableDataBuilder>().Sum(t => t.RowsCreated); }
        }

        public IItemFieldLookup FieldLookup { get; set; }
        
        public DataProcessor(ITableMapper tableMapper,
            ITableDataBatchWriter batchWriter = null,
            int batchSize = 10000)
        {
            TableMap = new TableMap();
            TableMapper = tableMapper;
            BatchSize = batchSize;
            BatchWriter = batchWriter;            
        }

        private bool _initialized = false;
        public void Initialize()
        {
            if( _initialized) throw new InvalidOperationException("Processor already initialized");
            _initialized = true;
            TableMapper.Initialize(this, null);
        }


        public void Process(IEnumerable data)
        {
            if (!_initialized)
            {
                Initialize();
            }
            var scope = new ProcessingScope()
            {                
                FieldLookup = FieldLookup
            };

            scope.Set(WrapItems(data));

            TableMapper.Process(scope);
        }

        IEnumerable<object> WrapItems(IEnumerable data)
        {
            foreach (var item in data.Cast<object>())
            {
                _tablesFinalized = false;
                if (BatchWriter != null && TableMap.Tables.Sum(t => t.RowCount) >= BatchSize)
                {
                    Flush();
                }
                if (BatchWriter == null || !BatchWriter.End)
                {
                    yield return item;
                }
            }
        }

        public void Flush()
        {
            if (TableMap.Tables.Any(t => t.RowCount > 0))
            {
                FinalizeTables();
                if (BatchWriter != null)
                {
                    BatchWriter.WriteBatch(TableMap.Tables);
                    Clear();
                }
            }
        }


        public void Clear()
        {
            foreach (var tb in TableMap.Tables)
            {
                ((TableDataBuilder)tb).Clear();
            }
        }

        private bool _tablesFinalized;
        void FinalizeTables()
        {
            if (!_tablesFinalized)
            {
                _tablesFinalized = true;
                foreach (var table in TableMap.Tables.OfType<TableDataBuilder>())
                {
                    table.FinalizeData();
                }
            }
        }


        public IEnumerable<TableData> Tables
        {
            get
            {
                FinalizeTables();
                return BatchWriter != null ? MergedTableData.FromTableSets(new[] { TableMap.Tables, BatchWriter.Tables }) : TableMap.Tables;
            }
        }

    }
}