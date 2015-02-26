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
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Mapping.Splitting;
using ExperienceExtractor.Processing.DataSources;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("split", "Splitted fields", "Splits the scope's current item with the specified splitter and includes fields from the specified field mappers in the table for each of the splitter's splits prefixed by their names."),
        ParseFactoryParameter("Splitter", typeof(ISplitter), "The splitter to split fields by"),
        ParseFactoryParameter("Fields", typeof(IEnumerable<IFieldMapper>), "The field mappers to include for each split")]
    public class SplitMapperFactory : IParseFactory<IFieldMapper>, IParseFactory<IDataFilter>, IParseFactory<ITableMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var splitter = parser.ParseSplitter(state.Select("Splitter") ?? state);

            return new SplittingFieldMapper(splitter, postfix =>            
                state.Postfix(postfix).SelectMany("Fields").Select(parser.ParseFieldMapper).ToArray()
            );
        }

        IDataFilter IParseFactory<IDataFilter>.Parse(JobParser parser, ParseState state)
        {
            var splitter = parser.ParseSplitter(state.Select("Splitter") ?? state);
            var filter = splitter as IDataFilter;
            if (filter== null)
            {
                throw new Exception(filter.GetType().Name + " does not implement IDataFilter");
            }

            return filter;
        }

        ITableMapper IParseFactory<ITableMapper>.Parse(JobParser parser, ParseState state)
        {
            var splitter = parser.ParseSplitter(state.Select("Splitter") ?? state);
            
            return new SplittingTableMapper(splitter, name => parser.ParseTableMapper(state.Postfix(name).Select("Table", true)));
        }
    }
}
