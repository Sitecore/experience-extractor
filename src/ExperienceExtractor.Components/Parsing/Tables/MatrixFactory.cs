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
using System.Collections.Generic;
using System.Linq;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Components.Parsing.Helpers;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Mapping.Splitting;
using ExperienceExtractor.Processing;

namespace ExperienceExtractor.Components.Parsing.Tables
{
    [ParseFactory("matrix", "Matrix", "Creates a matrix of the specified type in the form of an adjacency list for the pages, events or goals in a vist"),
    ParseFactoryParameter("Select", typeof(string),  "Pages, Events or Goals", "Pages", isMainParameter: true),
        ParseFactoryParameter("Type", typeof(string),  "Cooccurrence: Items that occured together, Links: Items where the first lead to the next"),
        ParseFactoryParameter("Fields", typeof(IEnumerable<IFieldMapper>), "The properties of each of the two items to select. Columns will be prefixed with '1' and '2'"),
        ParseFactoryParameter("CommonFields", typeof(IEnumerable<IFieldMapper>), "The facts and dimensions to include common to the two items")]
    public class MatrixFactory : IParseFactory<ITableMapper>
    {
        public ITableMapper Parse(JobParser parser, ParseState state)
        {
            var source = state.TryGet<string>("Select", mainParameter: true) ?? "Pages";

            var type = state.TryGet("Type", "CoOccurrence");

            var name = state.TryGet("Name", source + type);

            var selector = Selectors.SelectFromName(source);

            Func<ProcessingScope, IEnumerable<Tuple<object, object>>> matrix;
            if (type.Equals("cooccurrence", StringComparison.InvariantCultureIgnoreCase))
            {
                matrix = scope=>MatrixTypes.CoOcurrence(selector(scope));
            } else if (type.Equals("links", StringComparison.InvariantCultureIgnoreCase))
            {
                matrix = scope => MatrixTypes.Links(selector(scope));
            }
            else
            {                
                throw state.AttributeError("Unkown matrix type '{0}'", type);
            }


            var facts = state.SelectMany("CommonFields").Select(parser.ParseFieldMapper).ToList();
            if (state.Select("CommonFields") == null)
            {
                facts.Add(new SimpleFieldMapper("Count", s=>1, typeof(int), FieldType.Fact));
            }

            return new MatrixTableMapper(state.AffixName(name), matrix, postfix =>
                state.Postfix(postfix).SelectMany("Fields", true).Select(parser.ParseFieldMapper).ToArray(), facts);

        }
    }
}
