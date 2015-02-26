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

namespace ExperienceExtractor.Data.Schema
{
    /// <summary>
    /// Defines a relation to another <see cref="TableDataSchema"/> in a <see cref="TableDataSchema"/>.
    /// </summary>
    public class TableDataRelation
    {
        /// <summary>
        /// The fields in the table containing this relation that are part of this relation
        /// </summary>
        public IList<Field> Fields { get; set; }

        /// <summary>
        /// The fields in the related table that are part of this relation
        /// </summary>
        public IList<Field> RelatedFields { get; set; }

        /// <summary>
        /// The table
        /// </summary>
        public TableDataSchema RelatedTable { get; set; }

        /// <summary>
        /// The type of the relation
        /// </summary>
        public RelationType RelationType { get; set; }
    }
}