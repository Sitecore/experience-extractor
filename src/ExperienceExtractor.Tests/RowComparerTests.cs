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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Tests
{
    [TestClass]
    public class RowComparerTests
    {
        [TestMethod]
        public void TestSingle()
        {
            var schema = CreateSchema(
                new Field { ValueType = typeof(int), FieldType = FieldType.Key, });

            var row1 = new object[] { 1 };
            var row2 = new object[] { 1 };
            var row3 = new object[] { 3 };

            var comparer = new RowComparer(schema);
            Assert.IsTrue(comparer.Equals(row1, row2));
            Assert.IsFalse(comparer.Equals(row1, row3));

            Assert.IsTrue(comparer.Compare(row1, row2) == 0);
            Assert.IsTrue(comparer.Compare(row1, row3) == -1);
            Assert.IsTrue(comparer.Compare(row3, row2) == 1);
        }

        [TestMethod]
        public void TestMulti()
        {
            var schema = CreateSchema(
                new Field { ValueType = typeof(string), FieldType = FieldType.Key },
                new Field { ValueType = typeof(int), FieldType = FieldType.Key },
                new Field { ValueType = typeof(DateTime?), FieldType = FieldType.Key },
                new Field { ValueType = typeof(int), FieldType = FieldType.Fact });

            var row1 = new object[] { "A", 1, null, 37 };
            var row2 = new object[] { "A", 1, null, 11 };
            var row3 = new object[] { "B", 1, null, 3 };
            var row4 = new object[] { "B", 1, new DateTime(2000, 1, 1), 9 };
            var row5 = new object[] { "B", 1, new DateTime(2000, 1, 1), 11 };
            var row6 = new object[] { "B", 1, new DateTime(2000, 1, 2), 3 };
            var row7 = new object[] { "B", 2, new DateTime(2000, 1, 2), 2 };
            var row8 = new object[] { "C", 19, new DateTime(2000, 1, 2), 2 };

            var comparer = new RowComparer(schema);
            Assert.IsTrue(comparer.Equals(row1, row2));
            Assert.IsFalse(comparer.Equals(row1, row4));
            Assert.IsTrue(comparer.Equals(row4, row5));
            Assert.IsFalse(comparer.Equals(row3, row4));

            Assert.IsTrue(comparer.Compare(row1, row2) == 0);
            Assert.IsTrue(comparer.Compare(row1, row3) == -1);
            Assert.IsTrue(comparer.Compare(row3, row2) == 1);
            Assert.IsTrue(comparer.Compare(row4, row3) == 1);
            Assert.IsTrue(comparer.Compare(row3, row4) == -1);
            Assert.IsTrue(comparer.Compare(row4, row5) == 0);
            Assert.IsTrue(comparer.Compare(row5, row6) == -1);
            Assert.IsTrue(comparer.Compare(row6, row7) == -1);
            Assert.IsTrue(comparer.Compare(row7, row8) == -1);
        }

        [TestMethod]
        public void TestDimensionKey()
        {
            var schema = CreateSchema(
                new Field { ValueType = typeof(int), FieldType = FieldType.Dimension, },
                new Field { ValueType = typeof(int), FieldType = FieldType.Dimension, },
                new Field { ValueType = typeof(int), FieldType = FieldType.Fact, });

            var row1 = new object[] { 1, 1, 1 };
            var row2 = new object[] { 1, 1, 4 };
            var row3 = new object[] { 1, 2, 1 };

            var comparer = new RowComparer(schema);
            Assert.IsTrue(comparer.Equals(row1, row2));
            Assert.IsFalse(comparer.Equals(row1, row3));

            Assert.IsTrue(comparer.Compare(row1, row2) == 0);
            Assert.IsTrue(comparer.Compare(row1, row3) == -1);
            Assert.IsTrue(comparer.Compare(row3, row2) == 1);
        }

        [TestMethod]
        public void TestUseKeyWhenPresent()
        {
            var schema = CreateSchema(
                new Field { ValueType = typeof(int), FieldType = FieldType.Key, },
                new Field { ValueType = typeof(int), FieldType = FieldType.Dimension, },
                new Field { ValueType = typeof(int), FieldType = FieldType.Fact, });

            var row1 = new object[] { 1, 1, 1 };
            var row2 = new object[] { 1, 2, 4 };
            var row3 = new object[] { 2, 3, 1 };

            var comparer = new RowComparer(schema);
            Assert.IsTrue(comparer.Equals(row1, row2));
            Assert.IsFalse(comparer.Equals(row1, row3));

            Assert.IsTrue(comparer.Compare(row1, row2) == 0);
            Assert.IsTrue(comparer.Compare(row1, row3) == -1);
            Assert.IsTrue(comparer.Compare(row3, row2) == 1);
        }



        static TableDataSchema CreateSchema(params Field[] fields)
        {
            return new TableDataSchema("Test") { Fields = fields };
        }


    }
}
