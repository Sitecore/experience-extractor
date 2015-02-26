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

namespace ExperienceExtractor.Processing.DataSources
{
    public abstract class DataSourceBase : IDataSource
    {
        public abstract IEnumerator GetEnumerator();
        public abstract long? Count { get; }
        public IList<IDataFilter> Filters { get; set; }        
        
        public event EventHandler<int> ItemLoaded;

        protected virtual void OnItemLoaded(int e)
        {
            EventHandler<int> handler = ItemLoaded;
            if (handler != null) handler(this, e);
        }

        protected DataSourceBase()
        {
            Filters = new List<IDataFilter>();
        }
    }
}
