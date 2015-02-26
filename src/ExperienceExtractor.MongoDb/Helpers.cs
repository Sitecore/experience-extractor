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
using ExperienceExtractor.Data;
using ExperienceExtractor.Export;
using ExperienceExtractor.MongoDb.Filters;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Sitecore.Analytics.Data.DataAccess.MongoDb;

namespace ExperienceExtractor.MongoDb
{
    public static class Helpers
    {

        public static IEnumerable<IEnumerable<T>> Batch<T>(
            this IEnumerable<T> source, int batchSize)
        {
            using (var enumerator = source.GetEnumerator())
                while (enumerator.MoveNext())
                    yield return YieldBatchElements(enumerator, batchSize - 1);
        }

        private static IEnumerable<T> YieldBatchElements<T>(
            IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (int i = 0; i < batchSize && source.MoveNext(); i++)
                yield return source.Current;
        }


        public static IEnumerable<MongoCursor<TType>> Split<TType>(this MongoDbCollection collection, 
            IMongoQuery query,
            int splits, Action<MongoCursor<TType>> initializer = null, double sampleStart = 0,
            double sampleEnd = 1)
        {
            var sampler = new MongoIdSampler();            
            var width = (sampleEnd - sampleStart)/splits;
            for (var i = 0; i < splits; i++)
            {
                var q = query ?? new QueryDocument();
                var start = sampleStart + i*width;
                var end = sampleStart + (i + 1)*width;
                if (start > 0 || end < 1)
                {
                    q = Query.And(sampler.GetIdRange(start, end), q);
                }

                var cursor = collection.FindAs<TType>(q);
                if (initializer != null) initializer(cursor);
                
                yield return cursor;
            }            
        }

        public static IEnumerable<T> FastSpool<T>(this MongoCursor<RawBsonDocument> cursor, int threads = 2)
        {            
            var binaryReaderSettings = new BsonBinaryReaderSettings();
            binaryReaderSettings.MaxDocumentSize = int.MaxValue;
            var serializer = BsonSerializer.LookupSerializer(typeof(T));
            return cursor
                .AsParallel().AsOrdered()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .WithDegreeOfParallelism(Math.Max(1, threads))
                .Select(doc =>
                {                    
                    using (doc)
                    {
                        return (T)serializer.Deserialize(new BsonBinaryReader(new BsonBuffer(doc.Slice, false), binaryReaderSettings),
                            typeof(T), null);
                    }
                });   
        }


        public static IEnumerable<TableData> Merge(this IEnumerable<TableData> tables, IEnumerable<TableData> otherTables)
        {
            var map = otherTables.ToDictionary(other => other.Schema.Name);
            
            foreach (var table in tables)
            {
                TableData other;
                if (map.TryGetValue(table.Schema.Name, out other))
                {
                    yield return new MergedTableData(table.Schema, new[] {table, other});
                    map.Remove(table.Schema.Name);
                }
                else
                {
                    yield return table;
                }
            }

            foreach (var table in map.Values)
            {
                yield return table;
            }
        }
    }
}
