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
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactoryParameter("Select", typeof(IFieldMapper), "Optional field mapper that selects the date/time field", defaultValue: "Visit's StartDateTime")]
    public abstract class DateTimeFieldFactoryBase : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var mapper = state.Select("Select").TryGet(parser.ParseFieldMapper) as SimpleFieldMapper;

            Func<ProcessingScope, DateTime?> selector =
                scope => scope.Current<IVisitAggregationContext>().TryGet(v => (DateTime?)v.Visit.StartDateTime);
            if (mapper != null)
            {
                selector = scope => (DateTime?)mapper.Selector(scope);
            }

            var defaultName = mapper != null ? mapper.Name : "StartDateTime";

            return Parse(selector, defaultName, parser, state);
        }

        protected abstract IFieldMapper Parse(Func<ProcessingScope, DateTime?> selector, 
            string defaultName,
            JobParser parser, ParseState state);
    }
}
