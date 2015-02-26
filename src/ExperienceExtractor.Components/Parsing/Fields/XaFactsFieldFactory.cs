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
using ExperienceExtractor.Components.Mapping.Sitecore;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("xafacts", "Facts as calculated in Experience Analytics"),
    ParseFactoryParameter("Types", typeof(FactTypes), "The facts to include as columns in the table. An array of any of the values Visits, Value, Bounces, Conversions, TimeOnSite, PageViews, Count", "All")]
    public class XaFactsFieldFactory : IParseFactory<IFieldMapper>
    {
        public static FactTypes ParseFactTypes(string[] factTypes)
        {       
            return factTypes.Length == 0
                ? FactTypes.All
                : (FactTypes) factTypes.Aggregate(0, (current, s) => current | (int) Enum.Parse(typeof (FactTypes), s, true));
           
        }

        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            return new XaFacts(state.AffixName, ParseFactTypes(state.TryGet("Types", new string[0], true)));
        }
    }
}
