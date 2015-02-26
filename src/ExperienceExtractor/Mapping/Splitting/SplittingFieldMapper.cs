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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping.Splitting
{
    public class SplittingFieldMapper : FieldMapperBase
    {
        private FieldMapperIterator[] _iterators;
        public ISplitter Splitter { get; private set; }
        public IFieldMapper[][] FieldMappers { get; private set; }

        public SplittingFieldMapper(ISplitter splitter, Func<string, IEnumerable<IFieldMapper>> fieldMapperFactory)
        {
            Splitter = splitter;
            FieldMappers = splitter.Names.Select(name => fieldMapperFactory(name).ToArray()).ToArray();            
        }

        public override void Initialize(DataProcessor processor)
        {
            foreach (var fm in FieldMappers.SelectMany(fm=>fm))
            {
                fm.Initialize(processor);
            }

            base.Initialize(processor);
        }

        protected override IEnumerable<Field> CreateFields()
        {
            var fields = FieldMappers.SelectMany(fm => fm.SelectMany(f => f.Fields)).ToArray();
            _iterators = FieldMappers.Select(fms => new FieldMapperIterator(fms, fields)).ToArray();

            return fields;
        }

        public override bool SetValues(ProcessingScope scope, IList<object> row)
        {
            var i = 0;
            var any = false;
            foreach (var split in Splitter.GetSplits(scope))
            {
                using (scope.Swap(split))
                {
                    any = _iterators[i++].SetValues(row, scope) || any;
                }
            }
            return any;
        }

        public override void InitializeRelatedTables(DataProcessor processor, TableDataBuilder table)
        {
            foreach (var fm in FieldMappers.SelectMany(fms => fms))
            {
                fm.InitializeRelatedTables(processor, table);
            }
        }

        public override void PostProcessRows(IEnumerable<IList<object>> rows)
        {
            foreach (var iterator in _iterators)
            {
                iterator.Apply(rows, (mapper, mapperRows) => mapper.PostProcessRows(mapperRows));
            }

            base.PostProcessRows(rows);
        }
    }
}
