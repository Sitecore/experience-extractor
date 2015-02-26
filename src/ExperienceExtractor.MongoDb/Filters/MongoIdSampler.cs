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
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Processing.DataSources;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace ExperienceExtractor.MongoDb.Filters
{    
    public class MongoIdSampler
    {
        static bool IsMax(double percentage)
        {
            return percentage >= 1 - 1e-16;
        }

        BsonBinaryData PercentageToMongoId(double percentage)
        {
            var number = IsMax(percentage) ? ulong.MaxValue : (ulong)(percentage * ulong.MaxValue);
            var bytes = BitConverter.GetBytes(number);
            ToBigEndian(bytes);

            return new BsonBinaryData(bytes.Concat(bytes).ToArray(), BsonBinarySubType.UuidLegacy, GuidRepresentation.CSharpLegacy);
        }

        void ToBigEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
        }

        //static string ToHex(byte[] bytes)
        //{
        //    var sb = new StringBuilder();
        //    foreach (var b in bytes)
        //    {
        //        sb.Append(b.ToString("x2"));
        //    }
        //    return sb.ToString();
        //}

        public bool GuidInRange(Guid id, double start, double end)
        {
            var mongoId = new BsonBinaryData(id, GuidRepresentation.CSharpLegacy);
            var cmp = PercentageToMongoId(start).CompareTo(mongoId);
            if (cmp > 0) return false;
            if (!IsMax(end))
            {
                return mongoId.CompareTo(PercentageToMongoId(end)) <= 0;
            }

            return true;
        }

        public IMongoQuery GetIdRange(double start, double end)
        {
            var query = Query.GTE("_id", PercentageToMongoId(start));
            if (!IsMax(end))
            {
                query = Query.And(query, Query.LT("_id", PercentageToMongoId(end)));
            }
            return query;
        }

        [ParseFactory("sample", "Limits the extracted visits to a deterministic random sample of the size specified. Sampling is based on interaction IDs, so this filter allows you to extract the \"first\" 10% and then the next 10% etc."),
        ParseFactoryParameter("Percentage", typeof(double), "The percentage of the complete data set to extract (number between 0 and 1)", required: true),
        ParseFactoryParameter("Offset", typeof(double), "Offset the sample by this percentage (number between 0 and 1)")]
        public class Factory : IParseFactory<IDataFilter>
        {
            public IDataFilter Parse(JobParser parser, ParseState state)
            {
                var p = state.Require<double>("Percentage", mainParameter: true);
                var offset = state.TryGet("Offset", 0d);

                return new MongoRandomSampleFilter(Math.Max(0, offset), Math.Min(1, offset + p));
            }
        }
    }
}
