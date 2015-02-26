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

namespace ExperienceExtractor.Processing.Labels
{
    public class LruCache<TKey, TValue>
    {
        public int MaxSize { get; set; }

        private readonly Dictionary<TKey, KeyValuePair<LinkedListNode<TKey>, TValue>> _cache 
            = new Dictionary<TKey, KeyValuePair<LinkedListNode<TKey>, TValue>>();

        private readonly LinkedList<TKey> _keyList = new LinkedList<TKey>();       

        public LruCache(int maxSize)
        {
            MaxSize = maxSize;
        }

        public void Clear()
        {
            _cache.Clear();
            _keyList.Clear();
        }

        public bool Contains(TKey key)
        {
            return _cache.ContainsKey(key);
        }              
        
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            if (MaxSize <= 0) return factory(key);

            KeyValuePair<LinkedListNode<TKey>, TValue> value;
            if (!_cache.TryGetValue(key, out value))
            {
                if (_cache.Count >= MaxSize)
                {
                    var first = _keyList.First;
                    _keyList.Remove(first);
                    _cache.Remove(first.Value);
                }
                value = new KeyValuePair<LinkedListNode<TKey>, TValue>(new LinkedListNode<TKey>(key), factory(key));
                _cache.Add(key, value);
            }
            else
            {                
                _keyList.Remove(value.Key);
            }
            _keyList.AddLast(value.Key);

            return value.Value;
        }

        public IEnumerable<TKey> Keys 
        {
            get { return _keyList; }
        } 
    }
}
