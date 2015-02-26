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
using System.Linq;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.Helpers;

namespace ExperienceExtractor.Mapping
{
    public class SimpleTableMapper : TableMapperBase
    {
        private readonly Func<ProcessingScope, IEnumerable> _selector;

        public SimpleTableMapper(string name, Func<ProcessingScope, IEnumerable> selector,
            IEnumerable<IFieldMapper> fieldMappers, IEnumerable<ITableMapper> tableMappers = null) :
            this(selector, new TableDefinition(name) { FieldMappers = fieldMappers.OrEmpty().ToList(), TableMappers = tableMappers.OrEmpty().ToList() })
        {
        }

        public SimpleTableMapper(Func<ProcessingScope, IEnumerable> selector,
            params TableDefinition[] tables) :
            base(tables)
        {
            _selector = selector ?? (ctx => (IEnumerable)ctx.CurrentObject);
        }
        
        public SimpleTableMapper(params TableDefinition[] tables) :
            this(null, tables)
        {
        }

        protected override IEnumerable SelectRowItems(ProcessingScope context)
        {
            return _selector(context);
        }

        public static SimpleTableMapper Inline(Func<ProcessingScope, IEnumerable> parentSelector, Func<ProcessingScope, IEnumerable> childSelector, params TableDefinition[] tables)
        {
            return new SimpleTableMapper(parentSelector, new TableDefinition(null)
            {
                TableMappers = new[] {new SimpleTableMapper(childSelector, tables)}
            });
        }
    }
}