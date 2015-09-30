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
using System.Linq;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping
{
    public abstract class FieldMapperBase : IFieldMapper, ICalculatedFieldContainer
    {
        private Field[] _fields;

        public IList<Field> Fields
        {
            get { return _fields; }
        }

        public virtual IEnumerable<CalculatedField> CalculatedFields
        {
            get { yield break; }   
        } 

        protected abstract IEnumerable<Field> CreateFields();


        public abstract bool SetValues(ProcessingScope scope, IList<object> target);


        public virtual void Initialize(DataProcessor processor)
        {
            _fields = CreateFields().ToArray();
        }

        public virtual void InitializeRelatedTables(DataProcessor processor, TableDataBuilder table)
        {
            
        }

        public virtual void PostProcessRows(IEnumerable<IList<object>> rows)
        {

        }
    }
}