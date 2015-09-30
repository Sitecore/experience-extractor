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
    /// An <see cref="IDataFilter"/> that filter objects of the type specified. If the object cannot be cast to this type the filter excludes the item
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public abstract class TypedDataFilter<TItem> : IDataFilter where TItem : class
    {
        public bool Include(object item)
        {
            var typedItem = item as TItem;
            return typedItem != null && Include(typedItem);
        }

        protected abstract bool Include(TItem item);


        public virtual bool IsStagingFilter { get { return false; } }
    }
}
