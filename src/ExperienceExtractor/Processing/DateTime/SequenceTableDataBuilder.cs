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

namespace ExperienceExtractor.Processing.DateTime
{
    public class SequenceTableDataBuilder<TKey> : TableDataBuilder
    {
        private readonly ISequenceMapper<TKey> _fieldMapper;
        private readonly IComparer<TKey> _comparer;

        private readonly HashSet<TKey> _seen = new HashSet<TKey>(); 

        public TKey Min { get; set; }
        public TKey Max { get; set; }

        public SequenceTableDataBuilder(string name, ISequenceMapper<TKey> fieldMapper, IComparer<TKey> comparer = null) : base(name, new[]{fieldMapper})
        {
            _fieldMapper = fieldMapper;
            _comparer = comparer ?? Comparer<TKey>.Default;
        }

        protected override bool SetValues(ProcessingScope context, object[] data)
        {
            var key = _fieldMapper.GetKeyFromContext(context);
            if (!Equals(key, null))
            {
                return SetValues(key, data);
            }

            return false;
        }

        protected virtual bool SetValues(TKey key, object[] data)
        {
            _seen.Add(key);
            if (Equals(Min, null) || _comparer.Compare(key, Min) < 0) Min = key;
            if (Equals(Max, null) || _comparer.Compare(Max, key) < 0) Max = key;

            return Iterator.Apply(data, (mapper, target) => ((ISequenceMapper<TKey>)mapper).SetValues(key, target));
        }

        public override void FinalizeData()
        {
            if (!Equals(Min, null) && !Equals(Max, null))
            {
                var value = Min;
                while (_comparer.Compare(value, Max) < 0)
                {
                    if (!_seen.Contains(value))
                    {
                        var data = CreateEmptyRow();
                        if (SetValues(value, data))
                        {
                            AddData(data);
                        }
                    }

                    value = _fieldMapper.Increment(value);
                }
            }

            base.FinalizeData();
        }
    }
}
