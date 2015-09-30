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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing.Helpers;
using ExperienceExtractor.Processing.Keys;
using Sitecore.Analytics.Model.Framework;
using Sitecore.Data.Indexing;

namespace ExperienceExtractor.Data
{
    public class RowComparer : IEqualityComparer<object[]>, IComparer<object[]>
    {
        private readonly List<Indexed<IComparer>> _comparers = new List<Indexed<IComparer>>();

        private readonly int[] _keys;

        private readonly IKeyFactory _rowHasher = new Fnv1a32();

        public RowComparer(TableDataSchema schema)
        {            
            foreach (var field in GetSortFields(schema))
            {
                _comparers.Add(new ComparerWrapper(field.Value.ValueType.GetComparer(),
                    field.Value.SortOrder == SortOrder.Descending)
                    .AsIndexed<IComparer>(field.Index));
            }                   

            _keys = schema.Fields.FindIndices(schema.IsKey).Select(ix => ix.Index).ToArray();
        }

        public bool Equals(object[] x, object[] y)
        {
            var areEqual = x == y
                || x != null && y != null
                    && (x.Length == y.Length && (_keys.Length == 0 || _keys.All(ix => Equals(x[ix], y[ix]))));

            return areEqual;
        }

        public int GetHashCode(object[] obj)
        {
            return _rowHasher.CalculateKey(_keys.Select(ix => obj[ix])).GetHashCode();
        }

        public int Compare(object[] x, object[] y)
        {
            foreach (var cmp in _comparers)
            {
                var v1 = x[cmp.Index];
                var v2 = y[cmp.Index];

                var c = v1 == null && v2 == null ? 0
                    : v1 == null ? -1
                    : v2 == null ? 1
                    : cmp.Value.Compare(v1, v2);

                if (c != 0)
                {
                    return c;
                }
            }

            return 0;
        }

        public static IEnumerable<Indexed<Field>> GetSortFields(TableDataSchema schema)
        {
            var fields = schema.Fields;            

            //Order by fields where SortOrder is specified, then by keys (always order by something)
            foreach (var ix in fields.AsIndexed().OrderBy(ix => ix.Value.SortOrder == SortOrder.Unspecified).ThenBy(ix => ix.Index))
            {
                var field = ix.Value;
                if (field.SortOrder != SortOrder.Unspecified || schema.IsKey(field))
                {
                    yield return ix;
                }
            }
        }

        class ComparerWrapper : IComparer
        {
            private readonly IComparer _inner;
            private readonly bool _descending;

            public ComparerWrapper(IComparer inner, bool descending)
            {
                _inner = inner;
                _descending = @descending;
            }

            public int Compare(object x, object y)
            {
                var c = _inner.Compare(x, y);
                return _descending ? -1 * c : c;
            }
        }
    }
}
