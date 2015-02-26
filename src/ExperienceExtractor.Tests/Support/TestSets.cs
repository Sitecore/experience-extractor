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
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Model.Entities;

namespace ExperienceExtractor.Tests.Support
{
    static class TestSets
    {

        public static IEnumerable<IVisitAggregationContext> Countries(int count, int countries, DateTime? start = null, TimeSpan? offset = null, int regionsPerCountry = 3)
        {
            start = start ?? new DateTime(2000, 1, 1);
            offset = offset ?? TimeSpan.Zero;

            var baseData = new VisitData();

            var date = start.Value;
            return Enumerable.Range(1, count).Select(i =>
            {
                var countryName = "C" + ((i / regionsPerCountry) % countries);
                var regionName = countryName + "_R" + i % regionsPerCountry;
                var vd = baseData.Clone().SetGeoData(countryName, regionName)
                    .Pages(date, TimeSpan.FromSeconds(3), 3, p =>
                        new[] { new PageEventData { PageEventDefinitionId = p.VisitPageIndex.ToGuid(), Value = p.VisitPageIndex * p.VisitPageIndex } });

                date = date.Add(offset.Value);

                vd.InteractionId = i.ToGuid();
                return vd.AsContext();
            });
        }


        private static IVisitAggregationContext AsContext(this VisitData visitData)
        {
            return new SimpleContext { Visit = visitData.UpdateSums() };
        }

        private static VisitData SetGeoData(this VisitData vd, string country, string region)
        {
            vd.GeoData = new WhoIsInformation { Country = country, Region = region };
            return vd;
        }

        private static VisitData Pages(this VisitData data, DateTime start, TimeSpan duration, int count,
            Func<PageData, IEnumerable<PageEventData>> events = null)
        {
            data.Pages = new List<PageData>();
            var date = start;
            data.StartDateTime = date;
            for (var i = 1; i <= count; i++)
            {
                var page = new PageData
                {
                    DateTime = date,
                    VisitPageIndex = i,
                    Item = new ItemData { Id = i.ToGuid() },
                    Duration = (int)duration.TotalSeconds
                };
                if (events != null)
                {
                    page.PageEvents = events(page).ToList();
                }

                data.Pages.Add(page);

                date = date.Add(duration);
            }
            data.EndDateTime = date;

            return data;
        }

        private static VisitData UpdateSums(this VisitData visitData)
        {
            visitData.Pages = visitData.Pages ?? new List<PageData>();

            visitData.VisitPageCount = visitData.Pages.Count;

            for (var i = 0; i < visitData.Pages.Count; i++)
            {
                if (i == 0)
                {
                    if (visitData.StartDateTime == default(DateTime))
                    {
                        visitData.StartDateTime = visitData.Pages[i].DateTime;
                    }
                }
                else
                {
                    visitData.Pages[i - 1].Duration = (int)(visitData.Pages[i].DateTime - visitData.Pages[i - 1].DateTime).TotalSeconds;
                }

                visitData.Pages[i].VisitPageIndex = i + 1;
                visitData.Pages[i].PageEvents = visitData.Pages[i].PageEvents ?? new List<PageEventData>();
            }

            if (visitData.Pages.Count > 0 && visitData.EndDateTime == default(DateTime))
            {
                visitData.EndDateTime = visitData.Pages.Last().DateTime.AddSeconds(visitData.Pages.Last().Duration);
            }

            visitData.Value = visitData.Pages.Sum(pd => pd.PageEvents == null ? 0 : pd.PageEvents.Sum(pe => pe.Value));

            return visitData;
        }

        class SimpleContext : IVisitAggregationContext
        {
            public T Get<T>(object key) where T : class
            {
                throw new NotImplementedException();
            }

            public VisitData Visit { get; set; }
            public IContact Contact { get; set; }
            public VisitData FirstVisit { get; set; }
        }
    }
}
