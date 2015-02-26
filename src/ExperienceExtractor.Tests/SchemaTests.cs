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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Tests.Support;

namespace ExperienceExtractor.Tests
{
    [TestClass]
    public class SchemaTests
    {

        [TestMethod]
        public void TestSchemaFieldCache()
        {
            var fields = new[]
            {
                new Field {FieldType = FieldType.Key, Name = "0"},
                new Field {FieldType = FieldType.Fact, Name = "1"},
                new Field {FieldType = FieldType.Dimension, Name = "2"},
                new Field {FieldType = FieldType.Label, Name = "3"},
                new Field {FieldType = FieldType.Key, Name = "4"},
                new Field {FieldType = FieldType.Fact, Name = "5"},
                new Field {FieldType = FieldType.Dimension, Name = "6"}
            };

            var schema = CreateSchema(fields);

            Assert.IsTrue(schema.Keys.Select(k => k.Index).SequenceEqual(new[] { 0, 4 }));
            Assert.IsTrue(schema.Keys.Select(k => k.Value).SequenceEqual(new[] { fields[0], fields[4] }));

            Assert.IsTrue(schema.Dimensions.Select(k => k.Index).SequenceEqual(new[] { 2, 6 }));
            Assert.IsTrue(schema.Dimensions.Select(k => k.Value).SequenceEqual(new[] { fields[2], fields[6] }));

            Assert.IsTrue(schema.Labels.Select(k => k.Index).SequenceEqual(new[] { 3 }));
            Assert.IsTrue(schema.Labels.Select(k => k.Value).SequenceEqual(new[] { fields[3] }));

            Assert.IsTrue(schema.Facts.Select(k => k.Index).SequenceEqual(new[] { 1, 5 }));
            Assert.IsTrue(schema.Facts.Select(k => k.Value).SequenceEqual(new[] { fields[1], fields[5] }));

            fields = new[]
            {
                new Field {FieldType = FieldType.Key, Name = "4"},
                new Field {FieldType = FieldType.Fact, Name = "5"}                
            };
            schema.Fields = fields;

            Assert.IsTrue(schema.Keys.Select(k => k.Index).SequenceEqual(new[] { 0}));
            Assert.IsTrue(schema.Keys.Select(k => k.Value).SequenceEqual(new[] { fields[0]}));

            Assert.IsFalse(schema.Dimensions.Any());            

            Assert.IsFalse(schema.Labels.Any());

            Assert.IsTrue(schema.Facts.Select(k => k.Index).SequenceEqual(new[] { 1 }));
            Assert.IsTrue(schema.Facts.Select(k => k.Value).SequenceEqual(new[] {fields[1]}));
        }


        [TestMethod]
        public void TestTableDataBuilderFieldOrder()
        {
            var fields = new[]
            {
                new Field {FieldType = FieldType.Key, ValueType = typeof(int), Name = "0"},
                new Field {FieldType = FieldType.Fact,  ValueType = typeof(int), Name = "1"},
                new Field {FieldType = FieldType.Dimension, ValueType = typeof(int), Name = "2"},
                new Field {FieldType = FieldType.Label, ValueType = typeof(int), Name = "3"},
                new Field {FieldType = FieldType.Key,  ValueType = typeof(int),Name = "4"},
                new Field {FieldType = FieldType.Fact,  ValueType = typeof(int),Name = "5"},
                new Field {FieldType = FieldType.Dimension, ValueType = typeof(int), Name = "6"}
            };

            var builder = new TableDataBuilder("Test", fields.Select(f => new StaticFieldMapper(f, 0)));

            Assert.IsTrue(
                builder.Schema.Fields.SequenceEqual(new[] { fields[0], fields[4], fields[2], fields[3], fields[6], fields[1], fields[5] }));
        }

        [TestMethod]
        public void TestTableDataBuilderDefaultValues()
        {
            var fields = new[]
            {
                new Field {FieldType = FieldType.Key, ValueType = typeof(int), Name = "0"},
                new Field {FieldType = FieldType.Key,  ValueType = typeof(int?), Name = "1"},
                new Field {FieldType = FieldType.Key, ValueType = typeof(string), Name = "2"},
                new Field {FieldType = FieldType.Key, ValueType = typeof(DateTime), Name = "3"},
                new Field {FieldType = FieldType.Key,  ValueType = typeof(DateTime?),Name = "4"},                
            };

            var builder = new TableDataBuilder("Test", fields.Select(f => new StaticFieldMapper(f, 0)));
            var row = builder.CreateEmptyRow();

            Assert.IsTrue(
                row.SequenceEqual(new object[] {0, null, null, default(DateTime), null}));
        }

        static TableDataSchema CreateSchema(params Field[] fields)
        {
            return new TableDataSchema("Test") { Fields = fields };
        }
    }
}
