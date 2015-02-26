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

namespace ExperienceExtractor.Processing
{
    public class ArrayMap<TValue> : IList<TValue>
    {
        public IList<TValue> Target { get; set; }

        public int[] TargetIndices { get; set; }


        public IEnumerator<TValue> GetEnumerator()
        {
            return TargetIndices.Select(ix => Target[ix]).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(TValue item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(TValue item)
        {
            return this.Any(src => src.Equals(item));
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            var i = 0;
            foreach (var item in this)
            {
                array[arrayIndex + i++] = item;
            }
        }

        public bool Remove(TValue item)
        {
            throw new NotSupportedException();
        }

        public int Count { get; private set; }
        public bool IsReadOnly { get; private set; }
        public int IndexOf(TValue item)
        {
            var i = 0;
            foreach (var src in this)
            {
                if (src.Equals(item)) return i;
                i++;
            }
            return -1;
        }

        public void Insert(int index, TValue item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public TValue this[int index]
        {
            get { return Target[TargetIndices[index]]; }
            set { Target[TargetIndices[index]] = value; }
        }
    }
}
