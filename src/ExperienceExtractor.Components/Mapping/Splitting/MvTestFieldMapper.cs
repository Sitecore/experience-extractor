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

using System.Collections.Generic;
using System.Linq;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Model.Definitions;

namespace ExperienceExtractor.Components.Mapping.Splitting
{
    public class MvTestFieldMapper : FieldMapperBase
    {
        public int Variables { get; set; }
        public TestSet TestSet { get; set; }
        public string Name { get; set; }


        public MvTestFieldMapper(TestSet testSet, string name = "MvTest")
        {
            TestSet = testSet;
            Name = name;
        }

        protected override IEnumerable<Field> CreateFields()
        {                        
            var i = 1;
            foreach (var variable in TestSet.Variables)
            {
                yield return new Field
                {
                    Name = Name + "Var" + i,
                    ValueType = typeof(int?),
                    FieldType = FieldType.Dimension
                };
                yield return new Field
                {
                    Name = Name + "Var" + i + "Name",
                    ValueType = typeof(string),
                    FieldType = FieldType.Label
                };
                ++i;
            }

        }

        public override bool SetValues(ProcessingScope scope, IList<object> target)
        {
            var testPage = scope.Current<IVisitAggregationContext>().TryGet(v => v.Visit.Pages.FirstOrDefault(p => p.MvTest != null && p.MvTest.Id == TestSet.Id));
            if (testPage != null)
            {
                var i = 0;
                foreach (var variable in TestSet.Variables)
                {
                    var value = testPage.MvTest.Combination[i];
                    target[i * 2] = value;
                    target[i * 2 + 1] = variable.Values[value].Label;
                    ++i;
                }

                return true;
            }
            return false;
        }
    }
}
