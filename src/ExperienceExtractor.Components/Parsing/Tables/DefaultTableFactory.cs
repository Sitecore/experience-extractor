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
using System.Linq;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Components.Parsing.Helpers;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Components.Parsing.Tables
{
    [ParseFactory("tables", "Default table mapper", "Maps the table definitions specified to the current scope assuming it is IEnumerable.")]
    public class DefaultTableFactory : IParseFactory<ITableMapper>
    {
        public ITableMapper Parse(JobParser parser, ParseState state)
        {
            var tables = state.SelectMany().Select(parser.ParseTableDefinition).ToArray();
            return new SimpleTableMapper(scope => (IEnumerable)scope.CurrentObject, tables);
        }
    }


    [ParseFactory("pages", "Pages", "Enumerates the pages of the current visit in scope")]
    public class PagesTableFactory : SelectorTableFactory
    {
        public override string GetSelector(JobParser parser, ParseState state)
        {
            return "Pages";
        }
    }

    [ParseFactory("events", "Enumerates the events of the current page or visit in scope")]
    public class EventsTableFactory : SelectorTableFactory
    {
        public override string GetSelector(JobParser parser, ParseState state)
        {
            return "Events";
        }
    }

    [ParseFactory("goals", "Enumerates the goals of the current page or visit in scope")]
    public class GoalsTableFactory : SelectorTableFactory
    {
        public override string GetSelector(JobParser parser, ParseState state)
        {
            return "Goals";
        }
    }

    [ParseFactoryParameter("Table", typeof(TableDefinition), "The desired table definition", isMainParameter: true, required: true),
        ParseFactoryParameter("Expand", typeof(TableDefinition), "A field mapper that selects a delimitted value to unwind rows by. For example, the value '1|2|3|' for 'pages' will produce three rows for each page with the values '1', '2' and '3' respectively. Use the field mapper 'current' to get the value."),
        ParseFactoryParameter("Delimiter", typeof(TableDefinition), "The string to split the the value of 'expand' by")]
    public abstract class SelectorTableFactory : IParseFactory<ITableMapper>
    {
        public abstract string GetSelector(JobParser parser, ParseState state);

        public ITableMapper Parse(JobParser parser, ParseState state)
        {
            var table = parser.ParseTableDefinition(state.Select("Table") ?? state.Select(null, true));
            
            Type itemType;
            var selector = Selectors.SelectFromName(GetSelector(parser, state), out itemType);

            var expand = state.Select("Expand");
            if (expand != null)
            {
                var expandField = parser.ParseFieldMapper(expand) as SimpleFieldMapper;
                if (expandField == null)
                {
                    throw ParseException.AttributeError(expand, "Invalid field specified");
                }

                var delimiter = state.TryGet("Delimiter", "|");

                return new SimpleTableMapper(selector,
                    new TableDefinition("").Map(
                        new SimpleTableMapper(
                            scope =>
                                expandField.Selector(scope)
                                    .TryGet(v => "" + v, "")
                                    .Split(new[] { delimiter }, StringSplitOptions.RemoveEmptyEntries), table)));
            }

            return new SimpleTableMapper(selector, table);
        }
    }


}
