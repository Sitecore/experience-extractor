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
using System.Linq;
using ExperienceExtractor.MongoDb.Filters;
using ExperienceExtractor.Processing.DataSources;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Sitecore.Analytics.Data.DataAccess.MongoDb;

namespace ExperienceExtractor.MongoDb
{
    public abstract class MongoDbDataSource<TItem> : DataSourceBase
    {
        private readonly MongoDbCollection _collection;
        public int DeserializationThreads { get; set; }

        public event EventHandler<MongoCursor> CursorInitializing;
       

        public MongoDbDataSource(MongoDbCollection collection, int threads = 2)
        {
            _collection = collection;
            DeserializationThreads = threads;            
        }

        private MongoCursor<RawBsonDocument> CreateCursor(bool countQuery)
        {
            IMongoQuery query = null;
            var mongoFilters =
                Filters.OfType<IMongoNativeFilter>();
            if (countQuery)
            {
                mongoFilters = mongoFilters.Where(f => (f as IEstimatedCountFilter) == null);
            }

            foreach (var filterQuery in mongoFilters.Select(f => f.MongoQuery)
                    .Where(f => f != null))
            {
                query = query != null ? Query.And(filterQuery, query) : filterQuery;
            }

            var cursor = query != null
                ? _collection.FindAs<RawBsonDocument>(query)
                : _collection.FindAllAs<RawBsonDocument>();


            foreach (var filter in mongoFilters)
            {
                filter.UpdateCursor(cursor);
            }

            OnCursorInitializing(cursor);

            return cursor;
        }

        public override long? Count
        {
            get
            {
                var population = (long?)CreateCursor(true).Size();
                foreach (var filter in Filters.OfType<IEstimatedCountFilter>())
                {
                    population = filter.EstimateCount(population);
                }
                return population;
            }
        }

        protected virtual void OnCursorInitializing(MongoCursor e)
        {
            EventHandler<MongoCursor> handler = CursorInitializing;
            if (handler != null) handler(this, e);
        }        


        protected virtual object Adapt(TItem item)
        {
            return item;
        }        

        public override IEnumerator GetEnumerator()
        {            
            var nonNativeFilters = Filters.Where(f => (f as IMongoNativeFilter) == null).ToArray();

            var processed = 0;
            foreach (var dbItem in CreateCursor(false).FastSpool<TItem>(DeserializationThreads))
            {
                var item = Adapt(dbItem);
                OnItemLoaded(++processed);

                if (nonNativeFilters.All(f => f.Include(item)))
                {
                    yield return item;
                }
            }
        }
    }
}
