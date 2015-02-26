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

using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;

namespace ExperienceExtractor.MongoDb.Filters
{

    public class MongoLimitFilter : IMongoNativeFilter
    {
        public int Count { get; set; }
        public int Skip { get; set; }

        public MongoLimitFilter(int count, int skip = 0)
        {
            Count = count;
            Skip = skip;
        }

        public bool Include(object item)
        {
            return true;
        }

        public IMongoQuery MongoQuery { get; private set; }
        public void UpdateCursor(MongoCursor cursor)
        {
            cursor.SetLimit(Count);
        }

        [ParseFactory("limit", "MongoDB Limit filter", "Limits the number of visits extracted from MongoDB"),
            ParseFactoryParameter("Count", typeof(int), "The number of visits to extract", isMainParameter: true),
            ParseFactoryParameter("Skip", typeof(int), "Skip this number of visits", "0")]
        public class Factory : IParseFactory<IDataFilter>
        {
            public IDataFilter Parse(JobParser parser, ParseState state)
            {
                return new MongoLimitFilter(state.Require<int>("Count", true), state.TryGet("Skip", 0));
            }            
        }
    }
}
