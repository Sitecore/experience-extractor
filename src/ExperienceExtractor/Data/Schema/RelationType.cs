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

namespace ExperienceExtractor.Data.Schema
{
    /// <summary>
    /// Relation types for <see cref="TableDataSchema"/>
    /// </summary>
    public enum RelationType
    {
        /// <summary>
        /// A dimension is referenced (0..1)
        /// </summary>       
        DimensionReference,

        /// <summary>
        /// A child table references this table (1..*)
        /// </summary>
        Child,

        /// <summary>
        /// Another table references this table as a dimension (1..*)
        /// </summary>
        Dimension,

        /// <summary>
        /// Another table references this table as a child in a parent/child relationship (1..1)
        /// </summary>
        Parent
    }
}