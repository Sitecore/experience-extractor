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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("count", "Aggregated row count", "The number of rows aggregated or the number of distinct parent rows in the aggregate, i.e. \"count distinct\" with respect to parent scope"),
        ParseFactoryParameter("Name", typeof(string), "The name of the field in the target table", defaultValue: "Count"),
        ParseFactoryParameter("Scope", typeof(int), "Parent offset. 0 corresponds to current scope, -1 to Parent, -2 to Parent.Parent etc.", "0")]
    public class CountFieldFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var name = state.AffixName(state.TryGet("Name", "Count", true));

            var scopeOffset = Math.Abs(state.TryGet("Scope", 0));
            
            return new SimpleFieldMapper(name, ctx =>
            {
                if (scopeOffset == 0) return 1;
                var p = ctx;
                for (var i = 0; i < scopeOffset && p != null; i++)
                {
                    p = p.Parent;
                }
                if (p != null)
                {
                    return ctx.OncePerScope(p, 1);
                }

                return 0;
            }, typeof(int), FieldType.Fact);
        }
    }
}
