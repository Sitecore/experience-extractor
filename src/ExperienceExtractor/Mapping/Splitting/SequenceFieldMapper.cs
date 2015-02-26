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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Mapping.Splitting
{
    public class SequenceFieldMapper : FieldMapperBase
    {
        public string Name { get; set; }
        public Func<ProcessingScope, IEnumerable> Selector { get; set; }
        public SequenceType Type { get; set; }

        public SequenceFieldMapper(string name, Func<ProcessingScope, IEnumerable> selector, SequenceType type = SequenceType.Path)
        {
            Name = name;
            Selector = selector;
            Type = type;
        }

        protected override IEnumerable<Field> CreateFields()
        {
            yield return new Field
            {
                Name = Name,
                FieldType = FieldType.Dimension,
                ValueType = typeof(string)
            };
        }

        public override bool SetValues(ProcessingScope scope, IList<object> target)
        {
            var values = Selector(scope);
            if (values == null) return false;

            var quotedValues = values.Cast<object>().Where(s => s != null).Select(s => "'" + s.ToString().Replace("'", @"\'") + "'").ToArray();

            if (Type == SequenceType.Path)
            {
                target[0] = string.Join(",", quotedValues);
            }
            else if (Type == SequenceType.Set)
            {
                target[0] = string.Join(",", new HashSet<string>(quotedValues).OrderBy(s => s));
            }
            else
            {
                target[0] = string.Join(",", quotedValues
                    .ToLookup(s => s).OrderBy(kv => kv.Key).Select(kv => kv.Key + ": " + kv.Count()));
            }

            return true;
        }
    }


}
