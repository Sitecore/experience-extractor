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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Tests.Support
{
    class StaticFieldMapper : IFieldMapper
    {
        public object Value { get; set; }

        private Field[] _fields;
        public StaticFieldMapper(Field field, object value)
        {
            Value = value;
            _fields = new[] {field};
        }

        public IList<Field> Fields { get { return _fields; }}


        public bool SetValues(ProcessingScope scope, IList<object> target)
        {
            if (Value != null)
            {
                target[0] = Value;
                return true;
            }
            return false;
        }

        public void Initialize(DataProcessor processor)
        {            
        }

        public void InitializeRelatedTables(DataProcessor processor, TableDataBuilder table)
        {            
        }

        public void PostProcessRows(IEnumerable<IList<object>> rows)
        {           
        }
    }
}
