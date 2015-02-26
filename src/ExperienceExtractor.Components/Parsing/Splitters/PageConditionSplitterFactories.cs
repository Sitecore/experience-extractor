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
using ExperienceExtractor.Components.Mapping.Splitting;
using ExperienceExtractor.Mapping.Splitting;
using ExperienceExtractor.Processing.DataSources;
using Sitecore.Analytics.Model;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;

namespace ExperienceExtractor.Components.Parsing.Splitters
{

    public static class PageConditionSplitterFactories
    {
        [ParseFactory("pageevent", "Page event", "What happened before/after a specific page event"),
            ParseFactoryParameter("EventId", typeof(Guid), "The event to split by. If omitted the event id is taken from context assuming an event in scope", isMainParameter: true)]
        public class PageEventSplitterFactory : IParseFactory<ISplitter>, IParseFactory<IDataFilter>
        {
            public ISplitter Parse(JobParser parser, ParseState state)
            {
                var eventIdString = state.TryGet<string>("EventId", mainParameter: true);

                var eventId = eventIdString != null ? Guid.Parse(eventIdString) : (Guid?) null;                

                return new PageConditionSplitter((context, page) =>
                {                    
                    var thisEventId = eventId;
                    if (!thisEventId.HasValue && context != null)
                    {
                        var e = context.Current<PageEventData>();
                        if (e != null)
                        {
                            thisEventId = e.PageEventDefinitionId;
                        }
                    }

                    return thisEventId.HasValue && page.PageEvents != null && page.PageEvents.Any(pe => pe.PageEventDefinitionId == thisEventId);
                });
            }

            IDataFilter IParseFactory<IDataFilter>.Parse(JobParser parser, ParseState state)
            {
                return Parse(parser, state) as IDataFilter;
            }
        }


        [ParseFactory("page", "Page", "What happened before/after a specific page"),
            ParseFactoryParameter("PageId", typeof(Guid), "The page to split by. If omitted it is taken from context assuming a page in scope", isMainParameter: true)]
        public class PageSplitterFactory : IParseFactory<ISplitter>, IParseFactory<IDataFilter>
        {
            public ISplitter Parse(JobParser parser, ParseState state)
            {
                var pageIdString = state.TryGet<string>("PageId", mainParameter: true);

                var pageId = pageIdString != null ? Guid.Parse(pageIdString) : (Guid?)null;

                return new PageConditionSplitter((context, page) =>
                {
                    var thisPageId = pageId;
                    if (!thisPageId.HasValue && context != null )
                    {
                        var e = context.Current<PageData>();
                        if (e != null)
                        {
                            thisPageId = e.Item.Id;
                        }
                        else
                        {
                            var d = context.Current<DimensionData>();
                            if (d != null)
                            {
                                Guid result;
                                thisPageId = Guid.TryParse(d.DimensionKey, out result) ? result : (Guid?) null;
                            }
                        }
                    }

                    return thisPageId.HasValue && page.Item.Id == thisPageId;
                });
            }

            IDataFilter IParseFactory<IDataFilter>.Parse(JobParser parser, ParseState state)
            {
                return Parse(parser, state) as IDataFilter;
            }
        }

        [ParseFactory("mvtest", "MV Test", "What happened before and after a visitor was exposed to a test the first time in a vist."),
            ParseFactoryParameter("TestId", typeof(Guid), "The ID of the test to split by")]
        public class MvTestSplitterFactory: IParseFactory<ISplitter>, IParseFactory<IDataFilter>
        {
            public ISplitter Parse(JobParser parser, ParseState state)
            {
                var testIdString = state.Require<string>("TestId", true);

                Guid testId;
                if (!Guid.TryParse(testIdString, out testId))
                {
                    throw state.AttributeError("Invalid test id specified ({0})", testIdString);
                }

                return new MvTestSplitter(testId);
            }

            IDataFilter IParseFactory<IDataFilter>.Parse(JobParser parser, ParseState state)
            {
                return Parse(parser, state) as IDataFilter;
            }
        }
    }
}
