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
using System.Reflection;

namespace ExperienceExtractor.Processing.Helpers
{
    public static class CollectionHelpers
    {
        public static IEnumerable<T> MergeDupplicates<T>(this IEnumerable<T> items, Func<T, T, T> mergeAction, IComparer<T> comparer = null) where T : class
        {
            comparer = comparer ?? Comparer<T>.Default;

            T prev = null;
            foreach (var item in items)
            {
                if (prev != null)
                {
                    var c = comparer.Compare(prev, item);
                    if (c > 0) throw new Exception("List must be sorted");

                    if (c == 0)
                    {
                        prev = mergeAction(prev, item);
                    }
                    else
                    {
                        yield return prev;
                        prev = item;
                    }
                }
                else
                {
                    prev = item;
                }
            }

            if (prev != null) yield return prev;
        }

        public static IEnumerable<T> MergeSorted<T>(this IEnumerable<T> first, IEnumerable<T> second, IComparer<T> comparer = null)
        {
            comparer = comparer ?? Comparer<T>.Default;

            using (var firstEnumerator = first.GetEnumerator())
            using (var secondEnumerator = second.GetEnumerator())
            {

                var elementsLeftInFirst = firstEnumerator.MoveNext();
                var elementsLeftInSecond = secondEnumerator.MoveNext();
                while (elementsLeftInFirst || elementsLeftInSecond)
                {
                    if (!elementsLeftInFirst)
                    {
                        do
                        {
                            yield return secondEnumerator.Current;
                        } while (secondEnumerator.MoveNext());
                        yield break;
                    }

                    if (!elementsLeftInSecond)
                    {
                        do
                        {
                            yield return firstEnumerator.Current;
                        } while (firstEnumerator.MoveNext());
                        yield break;
                    }

                    if (comparer.Compare(firstEnumerator.Current, secondEnumerator.Current) < 0)
                    {
                        yield return firstEnumerator.Current;
                        elementsLeftInFirst = firstEnumerator.MoveNext();
                    }
                    else
                    {
                        yield return secondEnumerator.Current;
                        elementsLeftInSecond = secondEnumerator.MoveNext();
                    }
                }
            }
        }

        public static IComparer GetComparer(this Type t)
        {
            return (IComparer)typeof(Comparer<>).MakeGenericType(t).GetProperty("Default", BindingFlags.Static | BindingFlags.Public).GetValue(null);
        }

        public static IEnumerable<Indexed<TValue>> FindIndices<TValue>(this IEnumerable<TValue> values, Func<TValue, bool> criteria)
        {
            var i = 0;
            foreach (var value in values)
            {
                if (criteria(value))
                {
                    yield return value.AsIndexed(i);
                }
                ++i;
            }
        }

        public static IEnumerable<Indexed<TValue>> AsIndexed<TValue>(this IEnumerable<TValue> values)
        {
            return values.Select((value, i) => new Indexed<TValue> { Index = i, Value = value });
        }

        public static Indexed<TValue> AsIndexed<TValue>(this TValue value, int index)
        {
            return new Indexed<TValue> { Index = index, Value = value };
        }


        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
        {
            TValue val;
            if (!dict.TryGetValue(key, out val))
            {
                dict.Add(key, val = factory());
            }
            return val;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue @default = default(TValue))
        {
            TValue val;
            return dict.TryGetValue(key, out val) ? val : @default;
        }

        public static IEnumerable OrEmpty(this IEnumerable items)
        {
            return items ?? Enumerable.Empty<object>();
        }

        public static IEnumerable<TItem> OrEmpty<TItem>(this IEnumerable<TItem> items)
        {
            return items ?? Enumerable.Empty<TItem>();
        }
    }
}
