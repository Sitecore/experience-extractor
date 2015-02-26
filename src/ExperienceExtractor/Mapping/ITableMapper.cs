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

using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;

namespace ExperienceExtractor.Mapping
{
    /// <summary>
    /// Classes implementing this interface define zero ore more tables in the output of an ETL job, and pushes objects to scope while a <see cref="DataProcessor"/> is processing.
    /// A table mapper can only be used by a single <see cref="DataProcessor"/>
    /// </summary>
    public interface ITableMapper
    {
        /// <summary>
        /// Adds and initializes the tables for this <see cref="ITableMapper"/>
        /// </summary>
        /// <param name="processor">The <see cref="DataProcessor"/> where this table mapper is used</param>
        /// <param name="parentTable">The table currently being initialized by the parent <see cref="ITableMapper"/></param>
        void Initialize(DataProcessor processor, TableDataBuilder parentTable);

        /// <summary>
        /// Adds rows to the <see cref="ITableMapper"/>'s tables from the objects currently in scope, and optionally pushes child objects to scope and passes control on to related <see cref="ITableMappers"/>.
        /// </summary>
        /// <param name="context"></param>
        void Process(ProcessingScope context);        
    }
}