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
using System.Configuration;
using System.Linq;
using ExperienceExtractor.MongoDb.Filters;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Sitecore.Analytics.Aggregation;
using Sitecore.Analytics.Data.DataAccess.MongoDb;
using Sitecore.Analytics.Model;
using ExperienceExtractor.Api.Http.Configuration;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Processing.DataSources;

namespace ExperienceExtractor.MongoDb
{
    public class MongoDbVisitAggregationContextSource : MongoDbDataSource<VisitData>
    {
        private readonly MongoDbDriver _driver;

        public MongoDbVisitAggregationContextSource(MongoDbDriver driver, int threads = 2)
            : base(driver.Interactions, threads)
        {
            _driver = driver;
        }


        public override void ApplyUpdateFilter(DateTime? lastSaveDate, DateTime? lastSaveDateEnd)
        {
            var remove = Filters.Where(filter => filter.IsStagingFilter).ToList();

            foreach (var filter in remove)
            {
                Filters.Remove(filter);
            }

            if (lastSaveDate.HasValue)
            {
                //DateRangeFilter is staging filter.
                Filters.Add(new MongoDateRangeFilter(lastSaveDate.Value.ToLocalTime(), lastSaveDateEnd, "SaveDateTime"));
            }
        }


        protected override object Adapt(VisitData item)
        {            
            if( item.Pages == null) item.Pages = new List<PageData>();
            foreach (var page in item.Pages)
            {
                if (page.PageEvents == null)
                {
                    page.PageEvents = new List<PageEventData>();
                }
            }

            return new MongoDbAggregationContext(_driver, item);
        }

        [ParseFactory("xdb",
            "MongoDB xDB connection",
            "Loads IVisitAggregationContexts from xDB limited by the filters specified"),

            ParseFactoryParameter("Connection", typeof(string),
                            "MongoDB connection string or name of connection string defined in <connectionStrings />"
                            , "The connection string defined in Experience Extractor's config file", true),

            ParseFactoryParameter("Filters", typeof(IEnumerable<IDataFilter>),
                            "Filters to limit the visits to extract"),

            ParseFactoryParameter("Index", typeof(string),
                            "Specific index to use to optimize extraction from MongoDB. Use the value '$natural' to scan all documents in MongoDB in insert order rather than loading them with an index. In situations where database size vastly exceeds available RAM this can produce faster results since accessing documents in index order may load a lot of pages in and out of RAM."),

            ParseFactoryParameter("Fields", typeof(string),
                            "Limit the fields returned from MongoDB for faster results. Note that entities will only be partially hydrated with the subset of fields specified which can give misleading results if fields expecting other values are included."),
            ]

        public class Factory : IParseFactory<IDataSource>
        {
            public IDataSource Parse(JobParser parser, ParseState state)
            {
                var connectionString = state.TryGet("connection", ExperienceExtractorWebApiConfig.XdbConnectionString, true);

                if (!connectionString.StartsWith("mongodb://"))
                {
                    var connection = ConfigurationManager.ConnectionStrings[connectionString];
                    if (connection == null)
                    {
                        throw state.AttributeError("Connection string '{0}' not found", connectionString);
                    }
                    connectionString = connection.ConnectionString;
                }

                var source = new MongoDbVisitAggregationContextSource(new MongoDbDriver(connectionString), ExperienceExtractorWebApiConfig.JobExecutionSettings.LoadThreads);

                //source.SecondaryLookup = state.TryGet("SecondaryLookup", false);

                var index = state.TryGet<string>("index");
                if (!string.IsNullOrEmpty(index))
                {
                    source.CursorInitializing += (sender, cursor) =>
                    {
                        if (index == "$natural")
                        {
                            cursor.SetSortOrder(SortBy.Ascending("$natural"));
                        }
                        else
                        {
                            cursor.SetHint(index);
                        }
                    };
                }

                var fields = state.TryGet("fields", new string[0]);
                if (fields.Length > 0)
                {
                    source.CursorInitializing += (sender, cursor) => cursor.SetFields(fields);
                }


                foreach (var filter in state.SelectMany("Filters"))
                {
                    source.Filters.Add(parser.ParseDataFilter(filter));
                }              

                return source;
            }

        }        
    }
}
