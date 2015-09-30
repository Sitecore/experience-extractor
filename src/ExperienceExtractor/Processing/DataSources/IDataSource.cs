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
using System.Collections;
using System.Collections.Generic;
using ExperienceExtractor.Api.Jobs;

namespace ExperienceExtractor.Processing.DataSources
{
    /// <summary>
    /// Classes implementing this interface provides the data to process in a <see cref="Job"/>
    /// </summary>
    public interface IDataSource : IEnumerable
    {
        /// <summary>
        /// The estimated number of items returned for processing before client side filters are applied
        /// </summary>
        long? Count { get; }

        /// <summary>
        /// Filters to limit the items returned for processing
        /// </summary>
        IList<IDataFilter> Filters { get; set; }

        /// <summary>
        /// This event is triggered every time an item is returned from the underlying data source before client side filters are applied. 
        /// The argument to the event handler is the current number of items returned from the underlying data source
        /// </summary>
        event EventHandler<int> ItemLoaded;


        //TODO: Figure out better name
        /// <summary>
        /// Adjusts filters to only include data >= lastSaveDate. This is used for incremental updates of postprocessor targets.
        /// </summary>
        /// <param name="lastSaveDate"></param>
        void ApplyUpdateFilter(System.DateTime? lastSaveDate, System.DateTime? lastSaveDateEnd);
    } 
}
