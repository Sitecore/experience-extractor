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
using ExperienceExtractor.Components.Parsing.Helpers;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Mapping.Splitting;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("sequence", "Maps a single attribute of  pages, events or goals to a single dimension column in a table. The output is enclosed in single quotes, e.g. 'Goal 1', 'Goal 2' etc."),
        ParseFactoryParameter("Select", typeof(string), "Pages, Events or Goals, optionally postfixed by properties, e.g. 'Pages.Url.Path' or 'Pages.Item.Id/@name'"),
        ParseFactoryParameter("Name", typeof(string), "Column name in table where included"),
        ParseFactoryParameter("Type", typeof(string), 
@"Possible values are
    - 'Path': The sequence of items in a visit in order
    - 'Set': Unique items in a visit
    - 'Count set': Unique items in a visit with count. The output format is JSON without enclosing braces, e.g. ""'Goal 1': 8, 'Goal 2': 2"" etc.")]
    public class SequenceFieldFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var source = state.TryGet("Select", "Pages", true);
            var path = source.Split('.');
            source = path[0];
            var name = state.TryGet("Name", source);

            Type itemType;
            var items = Selectors.SelectFromName(source, out itemType);

            var selector = string.Join(".", path.Skip(1));
            if( string.IsNullOrEmpty(selector))
            {
                selector = Selectors.DefaultSelector(source);
            }

            var getterInfo = parser.CompileGetter(itemType, selector);

            return new SequenceFieldMapper(name,
                scope=>items(scope).Select(item=>getterInfo.Getter(item, scope)),
                (SequenceType)
                    Enum.Parse(typeof (SequenceType), state.TryGet("Type", "Path"), true));
        }
    }
}
