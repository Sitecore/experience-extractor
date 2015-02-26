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
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping.Splitting
{
    /// <summary>
    /// Provides different splits (views) of an object in scope.    
    /// </summary>
    public interface ISplitter
    {
        /// <summary>
        /// The names of the splits
        /// </summary>
        string[] Names { get; }

        /// <summary>
        /// Gets the values for the splits given the current <see cref="ProcessingScope"/>
        /// </summary>
        /// <param name="scope">The current scope</param>
        /// <returns></returns>
        IEnumerable<object> GetSplits(ProcessingScope scope);
    }
}
