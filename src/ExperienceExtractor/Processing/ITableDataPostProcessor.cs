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
using ExperienceExtractor.Export;

namespace ExperienceExtractor.Processing
{
    /// <summary>
    /// Performs operations on the table data from a completed processing job 
    /// </summary>
    public interface ITableDataPostProcessor
    {

        /// <summary>
        /// A descriptive name for this <see cref="ITableDataPostProcessor"/>
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Processes the rows in the tables specified
        /// </summary>
        /// <param name="tempDirectory">The temporary directory where the output of a job is collected</param>
        /// <param name="tables">Tables containing the data from a completed job</param>
        void Process(string tempDirectory, IEnumerable<CsvTableData> tables);

        /// <summary>
        /// Validates the prerequisites for the post processing (e.g. external connections) before a job starts to give early feedback about errors. 
        /// </summary>
        void Validate();
    }
}
