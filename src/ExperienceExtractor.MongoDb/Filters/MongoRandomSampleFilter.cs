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

using ExperienceExtractor.Processing.DataSources;
using MongoDB.Driver;
using Sitecore.Analytics.Aggregation.Data.Model;

namespace ExperienceExtractor.MongoDb.Filters
{
    public class MongoRandomSampleFilter : TypedDataFilter<IVisitAggregationContext>, IMongoNativeFilter, IEstimatedCountFilter
    {
        public double Start { get; set; }
        public double End { get; set; }

        /// <summary>
        /// If the filter is only used for staging. 
        /// If true, scheduled updates will include all data (that is, rebuild on first run), if false, scheduled updates will use this sampling
        /// </summary>
        public bool ForStaging { get; set; }

        private MongoIdSampler _sampler = new MongoIdSampler();
        public MongoRandomSampleFilter(double start = 0, double end = 1)
        {
            Start = start;
            End = end;
        }

        public bool IsStagingFilter { get { return ForStaging; } }

        protected override bool Include(IVisitAggregationContext item)
        {
            if (Start > 0 || End < 1)
            {
                return _sampler.GuidInRange(item.Visit.InteractionId, Start, End);
            }
            return true;            
        }

        public IMongoQuery MongoQuery
        {
            get
            {
                if (Start > 0 || End < 1)
                {                    
                    return _sampler.GetIdRange(Start, End);
                }

                return null;
            }
        }

        public void UpdateCursor(MongoCursor cursor)
        {

        }

        public long? EstimateCount(long? population)
        {
            if (!population.HasValue) return null;

            return (long) (population.Value * (End - Start)/1d);
        }
    }
}
