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

using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Components.Mapping.Sitecore;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("facts", "Facts as calculated in Experience Analytics"),
    ParseFactoryParameter("Types", typeof(FactTypes), "The facts to include as columns in the table. An array with any of the values Visits, Value, Bounces, Conversions, TimeOnSite, PageViews, Count", "All"),
    ParseFactoryParameter("Prefix", typeof(string), "Prefix for that fact column names"),
    ParseFactoryParameter("EntireVisit", typeof(bool), "If false, facts are calculated relative to the current page or event. When true, facts are calculated for the entire visit.", defaultValue: "false")]
    public class FactFieldsFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var factTypes = XaFactsFieldFactory.ParseFactTypes(state.TryGet("Types", new string[0], true));

            var prefix = state.TryGet<string>("Prefix");            

            return new FactsMapper(factTypes, state.Prefix(prefix).AffixName, state.TryGet("EntireVisit", false));
        }
    }
}
