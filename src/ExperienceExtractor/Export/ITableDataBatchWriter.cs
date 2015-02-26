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
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Data;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Export
{
    /// <summary>
    /// Offloads data to a secondary storage while data is processed to preserve memory.
    /// </summary>
    public interface ITableDataBatchWriter : IDisposable
    {
        /// <summary>
        /// Writes a batch of tables
        /// </summary>
        /// <param name="tables">The tables to write</param>
        void WriteBatch(IEnumerable<TableData> tables);

        /// <summary>
        /// The merged tables from all the batches written
        /// </summary>
        IEnumerable<TableData> Tables { get; }

        /// <summary>
        /// A flag to indicate if prcoessing should end prematurely due to storage limit or other constraints.
        /// </summary>
        bool End { get; }
    }
}
