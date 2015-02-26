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
    /// Classes implementing this interface persists a list of tables and provides access to the persisted data as tables with the same schemas
    /// </summary>
    public interface ITableDataExporter
    {
        /// <summary>
        /// Persists the tables specified and returns tables with the same schemas access accessing the persisted data
        /// </summary>
        /// <param name="tables"></param>
        /// <returns></returns>
        IEnumerable<TableData> Export(IEnumerable<TableData> tables);        
    }
}
