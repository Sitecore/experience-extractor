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
using ExperienceExtractor.Components.Mapping.Sitecore;
using ExperienceExtractor.Components.Parsing.Fields;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Components.Parsing.Tables
{    
    [ParseFactory("xa", "Adds key and label field in a nested table from an Experience Analytics dimension that generates multiple rows per vist"),
        ParseFactoryParameter("Dimension", typeof(string), "Dimension's guid or path to dimension item in Sitecore. If a leading slash is omitted in the path it is relative to /sitecore/system/Marketing Control Panel/Experience Analytics/Dimensions/", isMainParameter: true, required: true),
        ParseFactoryParameter("Name", typeof(string), "The name of the nested table in the output", defaultValue: "[DimensionTypeName]"),
        ParseFactoryParameter("KeyName", typeof(string), "The column name for the key value", defaultValue: "[DimensionTypeName]Key"),
        ParseFactoryParameter("LabelName", typeof(string), "The column name for the label value", defaultValue: "[DimensionTypeName]Label"),
        ParseFactoryParameter("HashKey", typeof(bool), "Use hash key rather than dimension's (string) key", defaultValue: "true"),
        ParseFactoryParameter("Facts", typeof(string[]), "The facts to include in the table. See 'facts' field mapper for details."),
        ParseFactoryParameter("Facts", typeof(IEnumerable<IFieldMapper>), "Additional fields to include in the table (based on parent scopes).")]
    public class XaTableFactory : IParseFactory<ITableMapper>
    {
        public ITableMapper Parse(JobParser parser, ParseState state)
        {
            var dimension = state.ParseDimension();
            
            var extraFields = state.SelectMany("Fields").Select(parser.ParseFieldMapper);

            var name = state.TryGet("Name", dimension.GetType().Name);

            return new XaTableMapper(dimension,
                state.AffixName(name),
                state.TryGet<string>("Name"),
                state.TryGet<bool>("HashKey", true),
                keyName: state.TryGet<string>("KeyName"),
                labelName:state.TryGet<string>("LabelName"),
                additionalFields: extraFields,
                factTypes: XaFactsFieldFactory.ParseFactTypes(state.TryGet("Facts", new string[0])),
                labelProvider: XaLabelProvider.FromDimension(dimension, parser.DefaultLanguage)
                );
        }
    }
}
