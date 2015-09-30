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

using System.Collections.Generic;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping
{
    /// <summary>
    /// Defines one or more fields in a table, and provides values for them. A field mapper may add and populate dimension tables based on the scope for the main table.
    /// An <see cref="IFieldMapper"/> can only be used by a single <see cref="DataProcessor"/>.      
    /// </summary>
    public interface IFieldMapper
    {        
        /// <summary>
        /// The fields provided for a table by this <see cref="IFieldMapper"/>
        /// After initialization these must not change.
        /// </summary>
        IList<Field> Fields { get; }        
        
        /// <summary>
        /// Sets the values in a row for the fields provided by this <see cref="IFieldMapper"/>
        /// </summary>
        /// <param name="scope">The current <see cref="ProcessingScope"/></param>
        /// <param name="target">An array where each position corresponds to the fields provided by this <see cref="IFieldMapper"/></param>
        /// <returns><c>true</c> if any fields were set; <c>false</c> otherwise</returns>
        bool SetValues(ProcessingScope scope, IList<object> target);

        /// <summary>
        /// Initializes the <see cref="IFieldMapper"/> for <see cref="DataProcessor"/> for which it is associated
        /// </summary>
        /// <param name="processor">The <see cref="DataProcessor"/> where the <see cref="IFieldMapper"/> is associated</param>
        void Initialize(DataProcessor processor);

        /// <summary>
        /// Allows the <see cref="IFieldMapper"/> to initialize lookup tables related to the fields provided, after the main table has been initialized with this and possible other <see cref="IFieldMapper"/>s
        /// </summary>
        /// <param name="processor">The <see cref="DataProcessor"/> where the <see cref="IFieldMapper"/> is associated</param>
        /// <param name="table">The <see cref="TableDataBuilder"/> where this <see cref="IFieldMapper"/>'s fields has been added</param>
        void InitializeRelatedTables(DataProcessor processor, TableDataBuilder table);

        /// <summary>
        /// Allows the <see cref="IFieldMapper"/> to provide labels for fields after rows have been aggregated to reduce the number of lookups.
        /// </summary>
        /// <param name="rows">A list of rows where the elements in each row corresponds to the fields provided by this <see cref="IFieldMapper"/></param>
        void PostProcessRows(IEnumerable<IList<object>> rows);        
    }    
}