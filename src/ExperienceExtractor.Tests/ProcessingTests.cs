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
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;
using ExperienceExtractor.Export;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Tests.Support;

namespace ExperienceExtractor.Tests
{
    [TestClass]
    public class ProcessingTests
    {
        [TestMethod]
        public void RowPerVisitKey()
        {
            var table = TestSets.Countries(1000, 37).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Key("VisitId", s => s.Current<IVisitAggregationContext>().Visit.InteractionId))).FirstOrDefault();

            Assert.AreEqual(1000, table.Rows.Count());
        }

        [TestMethod]
        public void RowPerVisitHashKey()
        {
            var table = TestSets.Countries(1000, 37).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Dimension("VisitId", s => s.Current<IVisitAggregationContext>().Visit.InteractionId))).FirstOrDefault();

            Assert.AreEqual(1000, table.Rows.Count());
        }

        [TestMethod]
        public void FactAggregation()
        {
            var table = TestSets.Countries(1000, 37).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Key("VisitId", s => s.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Fact("Value", s => s.Current<IVisitAggregationContext>().Visit.Value))).FirstOrDefault();

            Assert.AreEqual(1000, table.Rows.Count());
            Assert.AreEqual(14000, table.Fields<int>("Value").Sum());
        }

        [TestMethod]
        public void Count()
        {
            var table = TestSets.Countries(1000, 37).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Count())).FirstOrDefault();
            Assert.AreEqual(1000, table.Field<int>("Count", table.Rows.First()));
        }

        [TestMethod]
        public void CountSum()
        {
            var table = TestSets.Countries(1000, 5).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Key("Country", s => s.Current<IVisitAggregationContext>().Visit.GeoData.Country)
                    .Count())).FirstOrDefault();

            var rows = table.Rows.ToList();
            Assert.AreEqual(5, rows.Count);
            Assert.AreEqual(1000, table.Fields<int>("Count").Sum());
        }

        [TestMethod]
        public void ChildTable()
        {
            var tables = TestSets.Countries(1000, 37).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Key("VisitId", s => s.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Fact("Value", s => s.Current<IVisitAggregationContext>().Visit.Value)
                    .Map(s => s.Current<IVisitAggregationContext>().Visit.Pages,
                        new TableDefinition("Pages")
                            .Key("Id", s => s.Current<PageData>().Item.Id))));

            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");
            var pages = tables.FirstOrDefault(t => t.Schema.Name == "Pages");

            Assert.AreEqual(1000, visits.Rows.Count());
            Assert.AreEqual(14000, visits.Fields<int>("Value").Sum());

            Assert.AreEqual(3000, pages.Rows.Count());
        }

        [TestMethod]
        public void NestedChildTables()
        {
            var tables = TestSets.Countries(1000, 37).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Key("VisitId", s => s.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Fact("Value", s => s.Current<IVisitAggregationContext>().Visit.Value)
                    .Map(s => s.Current<IVisitAggregationContext>().Visit.Pages,
                        new TableDefinition("Pages")
                            .Key("Id", s => s.Current<PageData>().Item.Id)
                            .Fact("Value", s => s.Current<PageData>().PageEvents.Sum(pe => pe.Value))
                            .Map(s => s.Current<PageData>().PageEvents,
                            new TableDefinition("Events")
                                .Dimension("Event", s => s.Current<PageEventData>().PageEventDefinitionId)
                                .Fact("Value", s => s.Current<PageEventData>().Value)))
                    .Map(s => new[] { s.Current<IVisitAggregationContext>().Visit.Pages.First() },
                        new TableDefinition("Pages2")
                            .Key("Id", s => s.Current<PageData>().Item.Id))));

            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");
            var pages = tables.FirstOrDefault(t => t.Schema.Name == "Pages");
            var pages2 = tables.FirstOrDefault(t => t.Schema.Name == "Pages2");
            var events = tables.FirstOrDefault(t => t.Schema.Name == "Events");


            Assert.AreEqual(1000, visits.Rows.Count());
            Assert.AreEqual(14000, visits.Fields<int>("Value").Sum());
            Assert.AreEqual(14000, pages.Fields<int>("Value").Sum());
            Assert.AreEqual(14000, events.Fields<int>("Value").Sum());

            Assert.AreEqual(3000, pages.Rows.Count());
            Assert.AreEqual(3000, events.Rows.Count());
            Assert.AreEqual(1000, pages2.Rows.Count());
        }

        [TestMethod]
        public void NestedChildTablesWithBatchingInCsv()
        {
            var csvDir = Path.Combine(Directory.GetCurrentDirectory(), "~tmp");
            if (Directory.Exists(csvDir)) Directory.Delete(csvDir, true);

            var csv = new CsvExporter(csvDir, binaryPartitions:true);

            var batchWriter = new TableDataBatchWriter(csv);

            //1000 visits.
            //1 row in Test per visit
            //3 rows in Events per visit
            //3 rows in Pages per visit
            //1 row in Pages2 per visit
            //--------------------------
            //8 rows per visit = 80000 in total

            //Batch size 26672 (ceiling(1000/3) visits) gives two disk partitions with 26672 and one with 26656 in memory

            var visitCount = 1000;
            var rowsPerVisit = 8;
            var visitPerBatch = (int)Math.Ceiling(visitCount / 3d);
            var rowsPerFilePartition = visitPerBatch * rowsPerVisit;
            var eventRowsPerVisit = 3;
            var expectedFilePartitions = 2;

            var tables = TestSets.Countries(visitCount, 37).Process(
                () => new SimpleTableMapper(new TableDefinition("Test")
                    .Key("VisitId", s => s.Current<IVisitAggregationContext>().Visit.InteractionId)
                    .Fact("Value", s => s.Current<IVisitAggregationContext>().Visit.Value)
                    .Map(s => s.Current<IVisitAggregationContext>().Visit.Pages,
                        new TableDefinition("Pages")
                            .Key("PageId", s => s.Current<PageData>().Item.Id)
                            .Fact("Value", s => s.Current<PageData>().PageEvents.Sum(pe => pe.Value))
                            .Map(s => s.Current<PageData>().PageEvents,
                                new TableDefinition("Events")
                                    .Dimension("Event", s => s.Current<PageEventData>().PageEventDefinitionId)
                                    .Fact("Value", s => s.Current<PageEventData>().Value)))
                    .Map(s => new[] { s.Current<IVisitAggregationContext>().Visit.Pages.First() },
                        new TableDefinition("Pages2")
                            .Key("Id", s => s.Current<PageData>().Item.Id))),
                initializer: p =>
                {
                    p.BatchWriter = batchWriter;
                    p.BatchSize = rowsPerFilePartition;
                });


            var partitions = new DirectoryInfo(csvDir).GetDirectories().Length;

            Assert.AreEqual(2, partitions, string.Format("{0:N0} rows should create 2 file partitions and one in memory", visitCount * rowsPerVisit));
            Assert.AreEqual(expectedFilePartitions * eventRowsPerVisit * visitPerBatch, batchWriter.Tables.FirstOrDefault(t => t.Schema.Name == "Events").Rows.Count(),
                string.Format("{0:N0} rows in event tables in file partitions", expectedFilePartitions * eventRowsPerVisit * visitPerBatch));

            Assert.AreEqual(expectedFilePartitions * rowsPerVisit * visitPerBatch, batchWriter.Tables.Sum(t => t.Rows.Count()),
                string.Format("{0:N0} total rows in file partitions", expectedFilePartitions * rowsPerVisit * visitPerBatch));

            Assert.AreEqual(visitCount*rowsPerVisit, tables.Sum(t => t.Rows.Count()), 
                string.Format("{0:N0} rows in file + memory partitions", visitCount*rowsPerVisit));

            //Merge partitions
            tables = csv.Export(tables);


            //Delete partitions
            batchWriter.Dispose();
            partitions = new DirectoryInfo(csvDir).GetDirectories().Length;
            Assert.AreEqual(0, partitions, "Temporary partition directories are deleted");


            var visits = tables.FirstOrDefault(t => t.Schema.Name == "Test");
            var pages = tables.FirstOrDefault(t => t.Schema.Name == "Pages");
            var pages2 = tables.FirstOrDefault(t => t.Schema.Name == "Pages2");
            var events = tables.FirstOrDefault(t => t.Schema.Name == "Events");


            Assert.AreEqual(1000, visits.Rows.Count());
            Assert.AreEqual(14000, visits.Fields<int>("Value").Sum());
            Assert.AreEqual(14000, pages.Fields<int>("Value").Sum());
            Assert.AreEqual(14000, events.Fields<int>("Value").Sum());

            Assert.AreEqual(3000, pages.Rows.Count());
            Assert.AreEqual(3000, events.Rows.Count());
            Assert.AreEqual(1000, pages2.Rows.Count());

            if (Directory.Exists(csvDir))
            {
                Directory.Delete(csvDir, true);
            }

        }


    }
}
