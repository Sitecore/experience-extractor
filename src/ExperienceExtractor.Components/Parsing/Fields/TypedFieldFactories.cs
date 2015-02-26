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
using ExperienceExtractor.Components.Parsing.Helpers;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    public class TypedFieldFactories
    {

        [ParseFactory("current", "Current item in scope", "Adds the current scope's item's string value (ToString) as a column")]
        public class CurrentFieldsFactory : ReflectionFactory<string>
        {
            public override string SuggestName(string suggestedName)
            {
                return "Current";
            }
            
            public override string SelectItem(ProcessingScope scope)
            {
                return scope.CurrentObject.TryGet(o => "" + o);
            }
        }

        [ParseFactory("visit", "Visit property", "Includes a property of the current visit in scope as a column in the table")]
        public class VisitFieldsFactory : ReflectionFactory<VisitData>
        {
            public override string DefaultPath
            {
                get { return "InteractionId"; }
            }

            public override VisitData SelectItem(ProcessingScope scope)
            {
                return scope.Current<IVisitAggregationContext>().TryGet(v => v.Visit);
            }
        }


        [ParseFactory("page", "Includes a property of the current page in scope as a column in the table")]
        public class PageFieldsFactory : ReflectionFactory<PageData>
        {
            public override string DefaultPath
            {
                get { return "Item.Id"; }
            }
        }        

        [ParseFactory("event", "Includes a property of the current event in scope as a column in the table")]
        public class EventFieldFactory : ReflectionFactory<PageEventData>
        {
            public override string DefaultPath
            {
                get { return "PageEventDefinitionId"; }
            }
        }
        
        [ParseFactoryParameter("Select", typeof(string), "The property to select (e.g. Url.Path). When the property is a Guid, add a slash '/' to select fields from the corresponding Sitecore item, e.g. Item.Id/Title or PageEventDefinitionId/@displayname"),
        ParseFactoryParameter("Labels", typeof(string[]), "Optional fields from the item to include as labels when the selected property is a guid")]
        public class ReflectionFactory<T> : IParseFactory<IFieldMapper>
            where T :class
        {

            public virtual string SuggestName(string suggestedName)
            {
                return suggestedName;
            }
            public virtual string DefaultPath { get { return ""; } }

            public virtual T SelectItem(ProcessingScope scope)
            {
                return scope.Current<T>();
            }

            public IFieldMapper Parse(JobParser parser, ParseState state)
            {
                var selector = state.TryGet("Select", DefaultPath, true);

                if (selector.StartsWith(":"))
                {
                    selector = DefaultPath + selector;
                }
                
                var getter = parser.CompileGetter(typeof (T), selector);

                var name = state.TryGet("Name", SuggestName(getter.SuggestedName));
                var fieldType = (FieldType) Enum.Parse(typeof(FieldType), state.TryGet("Type", "Dimension"), true);


                IFieldMapper mapper = new SimpleFieldMapper(state.AffixName(name),
                    scope => SelectItem(scope).TryGet(item => getter.Getter(item, scope), getter.DefaultValue),
                    getter.ValueType, fieldType);

                var labelState = state.Select("Labels");
                if (labelState != null)
                {
                    var labels = labelState.Keys.ToDictionary(labelState.AffixName, key =>
                        (ILabelProvider)
                            new SitecoreFieldLabelProvider(labelState.TryGet<string>(key), parser.DefaultLanguage));                                            
                    if (labels.Count > 0)
                    {
                        return new LabeledFieldMapper(mapper, labels);
                    }
                }

                return mapper;
            }
        }
    }
}
