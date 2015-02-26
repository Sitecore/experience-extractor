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
using ExperienceExtractor.Mapping.Time;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("date", "Date dimension", "Creates a date dimension table with consecutive dates from the extracted visits. The date table contains information such as year, month, day of week etc. localized a job's language."),
        ParseFactoryParameter("Name", typeof(string), "Name of date dimension table if not inlined", defaultValue: "StartDateTime"),
        ParseFactoryParameter("UseDateKey", typeof(bool), "Add date columns to main table rather than creating a separate dimension table"),
        ParseFactoryParameter("Inline", typeof(bool), "Add date columns to the main table rather than creating a separate dimension table"),
        ParseFactoryParameter("Resolution", typeof(string), "Resolution. Can be Year, Quarter, Month or Date"),
        ParseFactoryParameter("Key", typeof(string), "Specifices if the date field should be added as a key rather than a dimension in the main table")]
    public class DateFieldFactory : DateTimeFieldFactoryBase
    {
        protected override IFieldMapper Parse(Func<ProcessingScope, DateTime?> selector, string defaultName,
            JobParser parser, ParseState state)
        {           
            return new DateDimension(
                    state.AffixName(state.TryGet("Name", defaultName)),
                    selector,
                    useDateForKey: state.TryGet("DateKey", true),
                    inlineFields: state.TryGet("Inline", false),                    
                    cultureInfo: parser.DefaultLanguage.CultureInfo,
                    key: state.TryGet("Key", false),
                    detailLevel:
                         state.TryGet("Resolution", DateDetailLevel.Date, true));
        }
    }
}
