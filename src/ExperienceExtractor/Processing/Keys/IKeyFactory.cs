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

namespace ExperienceExtractor.Processing.Keys
{
    /// <summary>
    /// Provides surrogate keys for tables.
    /// </summary>
    public interface IKeyFactory
    {       
        /// <summary>
        /// Calculates the key from the values specified.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        object CalculateKey(IEnumerable<object> values);

        /// <summary>
        /// Returns a field for a table schema with a name and value type matching the keys this key factory generates.
        /// </summary>
        /// <param name="schema"></param>
        /// <returns></returns>
        Field GetKeyField(TableDataSchema schema);        
    }
}
