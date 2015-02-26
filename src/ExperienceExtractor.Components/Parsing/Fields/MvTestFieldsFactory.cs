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
using ExperienceExtractor.Components.Mapping.Splitting;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Mapping.Splitting;
using Sitecore.Analytics.Testing;

namespace ExperienceExtractor.Components.Parsing.Fields
{

    [ParseFactory("mvtest", "Multi variate test fields", "Adds a column with variable number and label for each variable in a MV test, and adds columns split by what happened before and after the test and in total"),
        ParseFactoryParameter("TestId", typeof(Guid), "The ID of the MV test in Sitecore"),
        ParseFactoryParameter("Fields", typeof(IEnumerable<IFieldMapper>), "Field mappers to split by Before/After/Total.")]
    public class MvTestFieldsFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            var testIdString = state.Require<string>("TestId", true);

            Guid testId;
            if (!Guid.TryParse(testIdString, out testId))
            {
                throw state.AttributeError("Invalid test id specified ({0})", testIdString);
            }

            
            var testSet = TestManager.GetTestSet(testId);

            var testMapper = new MvTestFieldMapper(testSet, state.AffixName(state.TryGet("Name", "MvTest")));
                
            if (state.SelectMany("Fields").Any())
            {
                var splitter = new SplittingFieldMapper(new MvTestSplitter(testId),
                    postfix =>
                        state.Postfix(postfix).SelectMany("Fields").Select(parser.ParseFieldMapper).ToArray());

                return new FieldMapperSet(testMapper.Name, true, new IFieldMapper[] { testMapper, splitter }, state.AffixName);
            }

            return testMapper;
        }
    }
}
