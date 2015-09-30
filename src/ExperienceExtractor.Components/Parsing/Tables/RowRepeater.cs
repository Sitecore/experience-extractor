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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Microsoft.AnalysisServices;

namespace ExperienceExtractor.Components.Parsing.Tables
{
    /// <summary>
    /// Creates a number of random rows for each source record. Useful for performance testing.
    /// </summary>
    [ParseFactory("repeat", "Row randomizer")]
    public class RowRepeater : IParseFactory<ITableMapper>
    {
        public ITableMapper Parse(JobParser parser, ParseState state)
        {
            var repeats = state.TryGet("Repeats", 10);


            var repeatState = new State();

            var def = parser.ParseTableDefinition(state);
            def.FieldMappers = def.FieldMappers.Concat(new[]{
                new SimpleFieldMapper("Repeat", scope => "Value" + repeatState.Level, typeof (string), FieldType.Dimension)}).ToArray();

            return new SimpleTableMapper(scope => Repeat(scope, repeats, repeatState), def);
        }

        static IEnumerable<object> Repeat(ProcessingScope scope, int levels, State repeatState)
        {
            foreach (var item in ((IEnumerable)scope.CurrentObject))
            {
                repeatState.Level = 1;
                for (var i = 0; i < levels; i++)
                {
                    yield return item;
                    ++repeatState.Level;
                }
            }
        }

        class State
        {
            public int Level { get; set; }
        }
    }
}
