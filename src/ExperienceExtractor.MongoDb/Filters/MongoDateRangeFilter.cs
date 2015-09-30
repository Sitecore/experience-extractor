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
using ExperienceExtractor.Processing.DateTime;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Newtonsoft.Json.Linq;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Processing.DataSources;

namespace ExperienceExtractor.MongoDb.Filters
{
    public class MongoDateRangeFilter : DateRangeFilter, IMongoNativeFilter
    {
        public string Field { get; set; }

        public MongoDateRangeFilter(DateTime? start = null, DateTime? end = null, string field = "StartDateTime")
            : base(start, end)
        {
            Field = field;
        }


        public IMongoQuery MongoQuery
        {
            get
            {                
                IMongoQuery dateQuery = null;
                if (Start.HasValue)
                {
                    dateQuery = Query.GTE(Field, Start.Value);
                }
                if (End.HasValue)
                {
                    var q = Query.LT(Field, End.Value);
                    dateQuery = dateQuery != null ? Query.And(dateQuery, q) : q;
                }

                return dateQuery;
            }
        }

        public void UpdateCursor(MongoCursor cursor)
        {

        }

        [ParseFactory("daterange", "Date range filter", "Limits the visits to extract to visits that started within the specified date range."),
        ParseFactoryParameter("Start", typeof(DateTime), "Start date ( StartDateTime >= this value)"),
        ParseFactoryParameter("End", typeof(DateTime), "End date ( StartDateTime < this value)")]

        public class Factory : IParseFactory<IDataFilter>
        {
            public IDataFilter Parse(JobParser parser, ParseState state)
            {
                var start = state.TryGet<DateTime?>("Start");
                var end = state.TryGet<DateTime?>("End");

                if (!start.HasValue && !end.HasValue)
                {
                    throw ParseException.AttributeError(state, "Expected start and/or end");
                }                

                var field = state.TryGet("Field", "StartDateTime");
                
                return new MongoDateRangeFilter(start, end, field);
            }            
        }
    }
}
