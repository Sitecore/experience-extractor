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

namespace ExperienceExtractor.Processing.DataSources
{
    /// <summary>
    /// Classes implementing this interface can be used to filter data from a <see cref="IDataSource"/>
    /// </summary>
    public interface IDataFilter
    {        
        /// <summary>
        /// Returns true if the item matches this filter; false, otherwise
        /// </summary>
        /// <param name="item">The item to evaluate this filter against</param>
        /// <returns></returns>
        bool Include(object item);
    }

    /// <summary>
    /// Classes implementing this interface provides an estimated count of the items after this filter has been applied without querying the underlying data source
    /// </summary>
    public interface IEstimatedCountFilter : IDataFilter
    {
        /// <summary>
        /// Estimates the number of source items will be included by this filter
        /// </summary>
        /// <param name="population">The number of source items before this filter is applied</param>
        /// <returns></returns>
        long? EstimateCount(long? population);
    }
}
