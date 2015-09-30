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

namespace ExperienceExtractor.Mapping
{
    /// <summary>
    /// Defines a table in the output.
    /// </summary>
    public class TableDefinition
    {
        /// <summary>
        /// The name of the table in the output
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The <see cref="IFieldMapper"/>s that define the fields in this table
        /// </summary>
        public IList<IFieldMapper> FieldMappers { get; set; }

        /// <summary>
        /// <see cref="ITableMapper"/>s that define tables related to this table
        /// </summary>
        public IList<ITableMapper> TableMappers { get; set; }


        public TableDefinition(string name)
        {
            Name = name;
            FieldMappers = new List<IFieldMapper>();
            TableMappers = new List<ITableMapper>();        
        }
    }
}
