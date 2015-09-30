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
    [ParseFactory("xa", "Experience Analytics dimension", "Adds key and label field from an Experience Analytics dimension that generates one row per vist"),
        ParseFactoryParameter("Dimension", typeof(string), "Dimension's guid or path to dimension item in Sitecore. If a leading slash is omitted in the path it is relative to /sitecore/system/Marketing Control Panel/Experience Analytics/Dimensions/", isMainParameter: true, required: true),
        ParseFactoryParameter("KeyName", typeof(string), "The column name for the key value", defaultValue: "[DimensionTypeName]Key"),
        ParseFactoryParameter("LabelName", typeof(string), "The column name for the label value", defaultValue: "[DimensionTypeName]Label")]
    public class XaFieldFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var dimension = state.ParseDimension();

            var keyName = state.AffixName(state.TryGet("KeyName", ()=>dimension.GetType().Name + "Key"));
            var labelName = state.AffixName(state.TryGet("LabelName", () => dimension.GetType().Name + "Label"));


            return new LabeledFieldMapper(new XaFieldMapper(dimension, false, keyName), labelName, 
                XaLabelProvider.FromDimension(dimension, parser.DefaultLanguage), friendlyName: XaFieldMapper.SuggestFriendlyLabelName(labelName));
        }        
    }
}
