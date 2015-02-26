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
    [ParseFactory("index", "Index of current item", "The index of the current item being processed relative to a parent scope"),
        ParseFactoryParameter("Name", typeof(string), "Name of the field in the target table", "Index"),
        ParseFactoryParameter("Scope", typeof(int), "Parent offset. -1 corresponds to Parent, -2 to Parent.Parent etc.", "-1")]
    public class IndexFieldFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var name = state.AffixName(state.TryGet("Name", "Index", true));

            var scopeOffset = Math.Abs(state.TryGet("Scope", -1, true));

            return new SimpleFieldMapper(name, ctx =>
            {
                if (scopeOffset == 0) return ctx.GlobalIndex;

                var p = ctx;
                for (var i = 0; i < scopeOffset && p != null; i++)
                {
                    p = p.Parent;
                }
                if (p != null)
                {
                    return ctx.Index(p);
                }

                return 0;
            }, typeof(int), FieldType.Dimension);
        }
    }
}
