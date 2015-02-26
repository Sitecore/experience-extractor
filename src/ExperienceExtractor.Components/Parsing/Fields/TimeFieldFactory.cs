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
    [ParseFactory("time", "Time of day dimension", "Creates a time dimension table with consecutive time of day from 00:00 to 24:00"),
        ParseFactoryParameter("Name", typeof(string), "Name of time dimension table if not inlined", defaultValue: "StartDateTimeTime"),
        ParseFactoryParameter("Inline", typeof(bool), "Add time columns to main table rather than creating a dimension table"),
        ParseFactoryParameter("Resolution", typeof(string), "Resolution. Can be Hour, Quarter or Minute")]
    public class TimeFieldFactory : DateTimeFieldFactoryBase
    {
        protected override IFieldMapper Parse(Func<ProcessingScope, DateTime?> selector, string defaultName,
            JobParser parser, ParseState state)
        {
            return new TimeDimension(
                state.AffixName(state.TryGet("Name", defaultName + "Time")),
                selector,
                inlineFields: state.TryGet<bool>("Inline"),
                cultureInfo: parser.DefaultLanguage.CultureInfo,
                detailLevel: state.TryGet("Resolution", TimeDetailLevel.Hour, true));
        }
    }
}
