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
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Processing
{
    public class FieldMapperIterator
    {
        private readonly ArrayMap<object> _map;

        private readonly FieldMapperFields[] _maps;
        public FieldMapperIterator(IEnumerable<IFieldMapper> fieldMappers, IEnumerable<Field> targetFields = null)
        {
            targetFields = targetFields ?? fieldMappers.SelectMany(mapper => mapper.Fields);

            var i = 0;
            var fieldIndices = targetFields.ToDictionary(field => field, field => i++);

            _maps = fieldMappers.Select(fm =>
                new FieldMapperFields
                {
                    FieldMapper = fm,
                    TargetIndices = fm.Fields.Select(ix => fieldIndices[ix]).ToArray()
                }
             ).ToArray();

            _map = new ArrayMap<object>();
        }

        public bool SetValues(IList<object> target, ProcessingScope context)
        {            
            return Apply(target, (mapper, mapperTarget) => mapper.SetValues(context, mapperTarget));            
        }

        public bool Apply(IList<object> target, Func<IFieldMapper, IList<object>, bool> action)
        {
            var any = false;
            _map.Target = target;
            foreach (var fm in _maps)
            {
                _map.TargetIndices = fm.TargetIndices;
                any = action(fm.FieldMapper, _map) || any;
            }
            return any;
        }

        public void Apply(IList<object> target, Action<IFieldMapper, IList<object>> action)
        {
            _map.Target = target;
            foreach (var fm in _maps)
            {
                _map.TargetIndices = fm.TargetIndices;
                action(fm.FieldMapper, _map);
            }
        }

        public void Apply(IEnumerable<IList<object>> targets, Action<IFieldMapper, IEnumerable<IList<object>>> action)
        {
            foreach (var fm in _maps)
            {                
                _map.TargetIndices = fm.TargetIndices;
                action(fm.FieldMapper, targets.Select(target =>
                {
                    _map.Target = target;
                    return _map;
                }));
            }
        }

        class FieldMapperFields
        {
            public IFieldMapper FieldMapper { get; set; }
            public int[] TargetIndices { get; set; }
        }
    }
}
