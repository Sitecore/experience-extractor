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
using System.Linq;
using ExperienceExtractor.Components.Mapping.Splitting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sitecore.Analytics.Aggregation.Data.Model;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Mapping.Splitting;
using ExperienceExtractor.Processing.Labels;
using ExperienceExtractor.Tests.Support;

namespace ExperienceExtractor.Tests
{
    [TestClass]
    public class FieldMappers
    {
        [TestMethod]
        public void Splitter()
        {
            var splitter = new PageConditionSplitter((scope, page) => page.VisitPageIndex == 2);

            var table = TestSets.Countries(1000, 37).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Key("VisitId", s => s.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Map(new SplittingFieldMapper(splitter, (name) =>
                    new[]{
                        new SimpleFieldMapper("Value" + name,
                            s =>
                                s.Current<IVisitAggregationContext>()
                                    .Visit.Value, typeof(int), FieldType.Fact)
                    })))).FirstOrDefault();

            Assert.AreEqual(1000, table.Rows.Count());
            Assert.AreEqual(5000, table.Fields<int>("ValueBefore").Sum());
            Assert.AreEqual(9000, table.Fields<int>("ValueAfter").Sum());
            Assert.AreEqual(14000, table.Fields<int>("ValueTotal").Sum());
        }

        [TestMethod]
        public void OneFieldHashLookup()
        {
            var tables = TestSets.Countries(1000, 9).Process(() =>
                new TableDefinition("Test")
                    .Dimension("VisitId", scope => scope.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Map(new FieldMapperSet("Country", false, new[]
                    {
                        new SimpleFieldMapper("Country",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Country, typeof (string),
                            FieldType.Dimension),
                    }))
                );

            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");
            var countries = tables.FirstOrDefault(t => t.Schema.Name == "Country");

            Assert.AreEqual(1000, visits.Rows.Count());
            Assert.AreEqual(9, countries.Rows.Count());
            Assert.AreEqual(9, visits.Fields<long>("CountryId").Distinct().Count());
        }

        [TestMethod]
        public void TwoFieldHashLookup()
        {
            var tables = TestSets.Countries(1000, 9, regionsPerCountry: 3).Process(() =>
                new TableDefinition("Test")
                    .Dimension("VisitId", scope => scope.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Map(new FieldMapperSet("Country", false, new[]
                    {
                        new SimpleFieldMapper("Country",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Country, typeof (string),
                            FieldType.Dimension),
                        new SimpleFieldMapper("Region",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Region, typeof (string),
                            FieldType.Dimension),
                    }))
                );

            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");
            var countries = tables.FirstOrDefault(t => t.Schema.Name == "Country");

            Assert.AreEqual(1000, visits.Rows.Count());
            Assert.AreEqual(27, countries.Rows.Count());
            Assert.AreEqual(27, visits.Fields<long>("CountryId").Distinct().Count());
        }

        [TestMethod]
        public void TwoFieldInlineLookup()
        {
            var tables = TestSets.Countries(1000, 9, regionsPerCountry: 3).Process(() =>
                new TableDefinition("Test")
                    .Dimension("VisitId", scope => scope.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Map(new FieldMapperSet("Country", true, new[]
                    {
                        new SimpleFieldMapper("Country",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Country, typeof (string),
                            FieldType.Dimension),
                        new SimpleFieldMapper("Region",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Region, typeof (string),
                            FieldType.Dimension),
                    }))
                );

            Assert.AreEqual(1, tables.Count());
            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");            

            Assert.AreEqual(1000, visits.Rows.Count());            
            Assert.AreEqual(9, visits.Fields<string>("Country").Distinct().Count());
            Assert.AreEqual(27, visits.Fields<string>("Region").Distinct().Count());
        }

        [TestMethod]
        public void TwoFieldKeyLookup()
        {
            var tables = TestSets.Countries(1000, 9, regionsPerCountry: 3).Process(() =>
                new TableDefinition("Test")
                    .Dimension("VisitId", scope => scope.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Map(new FieldMapperSet("Country", false, new[]
                    {
                        new SimpleFieldMapper("Country",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Country, typeof (string),
                            FieldType.Key),
                        new SimpleFieldMapper("Region",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Region, typeof (string),
                            FieldType.Key),
                    }))
                );

            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");
            var countries = tables.FirstOrDefault(t => t.Schema.Name == "Country");

            Assert.AreEqual(1000, visits.Rows.Count());
            Assert.AreEqual(27, countries.Rows.Count());
            Assert.AreEqual(9, visits.Fields<string>("Country").Distinct().Count());
            Assert.AreEqual(27, visits.Fields<string>("Region").Distinct().Count());
        }

        [TestMethod]
        public void OneFieldKeyLookup()
        {
            var tables = TestSets.Countries(1000, 9).Process(() =>
                new TableDefinition("Test")
                    .Dimension("VisitId", scope => scope.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Map(new FieldMapperSet("Country", false, new[]
                    {
                        new SimpleFieldMapper("Country",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Country, typeof (string),
                            FieldType.Key),
                    }))
                );

            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");
            var countries = tables.FirstOrDefault(t => t.Schema.Name == "Country");

            Assert.AreEqual(1000, visits.Rows.Count());
            Assert.AreEqual(9, countries.Rows.Count());
            Assert.AreEqual(9, visits.Fields<string>("Country").Distinct().Count());
        }

        [TestMethod]
        public void LookupInLookup()
        {
            var tables = TestSets.Countries(1000, 9).Process(() =>
                new TableDefinition("Test")
                    .Dimension("VisitId", scope => scope.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Map(new FieldMapperSet("Country", false, new IFieldMapper[]
                    {
                        new SimpleFieldMapper("Country",
                            scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Country, typeof (string),
                            FieldType.Dimension),
                        new FieldMapperSet("Country2", false, new []
                        {                            
                            new SimpleFieldMapper("Country",
                                scope => scope.Current<IVisitAggregationContext>().Visit.GeoData.Country, typeof (string),
                                FieldType.Dimension),   
                             new SimpleFieldMapper("Count", s=>1, typeof(int), FieldType.Fact),                 
                        }), 
                        
                    }))
                );

            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");
            var countries = tables.FirstOrDefault(t => t.Schema.Name == "Country");
            var countries2 = tables.FirstOrDefault(t => t.Schema.Name == "Country2");

            Assert.AreEqual(1000, visits.Rows.Count());
            Assert.AreEqual(9, countries.Rows.Count());
            Assert.AreEqual(9, countries2.Rows.Count());
            Assert.AreEqual(9, visits.Fields<long>("CountryId").Distinct().Count());
            Assert.AreEqual(9, countries.Fields<long>("Country2Id").Distinct().Count());
            Assert.AreEqual(1000, countries2.Fields<int>("Count").Sum());
        }

        [TestMethod]
        public void TestLruCache()
        {
            var cache = new LruCache<object, object>(5);

            Assert.AreEqual(1, cache.GetOrAdd(1, _ => 1));
            Assert.AreEqual(2, cache.GetOrAdd(2, _ => 2));
            Assert.AreEqual(3, cache.GetOrAdd(3, _ => 3));
            Assert.AreEqual(4, cache.GetOrAdd(4, _ => 4));
            Assert.AreEqual(5, cache.GetOrAdd(5, _ => 5));
            Assert.AreEqual(6, cache.GetOrAdd(6, _ => 6));

            Assert.IsTrue(cache.Keys.SequenceEqual(new object[] { 2, 3, 4, 5, 6 }));

            Assert.AreEqual(true, CacheLoaded(cache, 1, 1));
            Assert.IsTrue(cache.Keys.SequenceEqual(new object[] { 3, 4, 5, 6, 1 }));
            Assert.AreEqual(false, CacheLoaded(cache, 1, 1));
            Assert.IsTrue(cache.Keys.SequenceEqual(new object[] { 3, 4, 5, 6, 1 }));
            Assert.AreEqual(false, CacheLoaded(cache, 4, 4));
            Assert.IsTrue(cache.Keys.SequenceEqual(new object[] { 3, 5, 6, 1, 4 }));

            Assert.AreEqual(true, CacheLoaded(cache, 2, 2));
            Assert.IsTrue(cache.Keys.SequenceEqual(new object[] { 5, 6, 1, 4, 2 }));
        }

        static bool CacheLoaded(LruCache<object, object> cache, object key, object value)
        {
            var loaded = false;
            Assert.AreEqual(value, cache.GetOrAdd(key, _ =>
            {
                loaded = true;
                return value;
            }));

            return loaded;
        }
    }
}
