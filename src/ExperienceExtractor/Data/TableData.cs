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
using System.Data;
using ExperienceExtractor.Data.Schema;

namespace ExperienceExtractor.Data
{
    /// <summary>
    /// Base class for table data (rows for a <see cref="TableDataSchema"/>)
    /// </summary>
    public abstract class TableData
    {
        /// <summary>
        /// The table's schema
        /// </summary>
        public TableDataSchema Schema { get; private set; }

        /// <summary>
        /// The name of the table (from schema)
        /// </summary>
        public string Name { get { return Schema.Name; } }

        /// <summary>
        /// The rows in this table ordered by fields with sortorder specified, then by key/dimensions
        /// </summary>
        public abstract IEnumerable<object[]> Rows { get; }

        /// <summary>
        /// The number of rows in this table if it can be determined
        /// </summary>
        public abstract int? RowCount { get; }


        protected TableData(TableDataSchema schema)
        {
            Schema = schema;
        }


        /// <summary>
        /// Adds the values of the fact fields in the two rows specified
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public virtual object[] MergeFacts(object[] x, object[] y)
        {
            foreach (var f in Schema.Facts)
            {
                if (!Adders.ContainsKey(f.Value.ValueType))
                {
                    throw new InvalidOperationException(string.Format("Facts of type {0} are not supported", f.Value.ValueType));
                }
                var ix = f.Index;
                x[ix] = Adders[f.Value.ValueType](x[ix], y[ix]);
            }

            return x;
        }

        /// <summary>
        /// Returns an implementation of <see cref="IDataReader"/> to read the rows in this tabe
        /// </summary>
        /// <returns></returns>
        public IDataReader CreateReader()
        {
            return new TableDataReader(this);
        }

        /// <summary>
        /// Supported fact types and how to add them.
        /// </summary>
        private static readonly Dictionary<Type, Func<object, object, object>> Adders = new Dictionary<Type, Func<object, object, object>>
        {
            {typeof (byte), (x, y) => (byte) (x??0) + (byte) (y??0)},
            {typeof (short), (x, y) => (short) (x??0) + (short) (y??0)},
            {typeof (int), (x, y) => (int) (x??0) + (int) (y??0)},
            {typeof (long), (x, y) => (long) (x??0L) + (long) (y??0L)},
            {typeof (float), (x, y) => (float) (x??0f) + (float) (y??0f)},
            {typeof (double), (x, y) => (double) (x??0d) + (double) (y??0d)},
            {typeof (decimal), (x, y) => (decimal) (x??0m) + (decimal) (y??0m)},
            {typeof (TimeSpan), (x, y) => (TimeSpan) (x??TimeSpan.Zero) + (TimeSpan) (y??TimeSpan.Zero)}
        };
    }
}