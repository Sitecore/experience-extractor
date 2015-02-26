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
using ExperienceExtractor.Mapping;
using FieldType = ExperienceExtractor.Data.Schema.FieldType;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("static", "Static value", "A static value in the table"),
        ParseFactoryParameter("Value", typeof(string), "The value"),
        ParseFactoryParameter("Name", typeof(string), "The column name")]
    public class StaticFieldFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var value = state.Require<string>("Value", true);

            return new SimpleFieldMapper(state.TryGet("Name", value), scope => value, typeof (string), FieldType.Dimension);
        }
    }
}
