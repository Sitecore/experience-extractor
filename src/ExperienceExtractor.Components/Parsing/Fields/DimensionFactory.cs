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

using System.Collections.Generic;
using System.Linq;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("dimension", "Extract dimension table", "Extracts a dimension table with the fields specified. If none of the fields are keys a hash key is added."),
        ParseFactoryParameter("Name", typeof(string), "The name of the table to extract", required: true),
        //ParseFactoryParameter("Name", typeof(string), "The name of the table to extract", required: true),
        ParseFactoryParameter("AffixNames", typeof(bool), "Apply pre and post fix of the current parse scope to table and field names. Set to false to use a shared dimension table for multiple fields", defaultValue: "false"),
        ParseFactoryParameter("Prefix", typeof(string), "Prefix for the key reference(s) in the main table", defaultValue: ""),    
        ParseFactoryParameter("Fields", typeof(IEnumerable<IFieldMapper>), "The fields to extract in an dimension table", defaultValue: ""),
        ParseFactoryParameter("Key", typeof(bool), "Add reference fields as keys", defaultValue: "false"),
    ]

    public class DimensionFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var tableName = state.Require<string>("Name");            
            var inline = state.TryGet("Inline", false);
            
            var fieldState = state.Clone();

            var affix = state.TryGet("AffixNames", true);
            if (affix)
            {
                tableName = state.AffixName(tableName);
            }
            else
            {
                fieldState = fieldState.ClearAffix();
            }

            var fieldPrefix = state.TryGet("Prefix", "");


            return new FieldMapperSet(tableName, inline,
                fieldState.SelectMany("Fields").Select(parser.ParseFieldMapper), state.AffixName,
                fieldNamePrefix: fieldPrefix)
            {
                Key = state.TryGet("Key", false)
            };
        }
    }
}
