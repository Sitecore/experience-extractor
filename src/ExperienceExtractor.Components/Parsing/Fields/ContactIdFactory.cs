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
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using Sitecore.Analytics.Aggregation.Data.Model;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    [ParseFactory("contactid", "Contact ID", "Contact ID for unique visitor count")]
    public class ContactIdFactory : IParseFactory<IFieldMapper>
    {
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            return new SimpleFieldMapper(state.AffixName("ContactId"), scope => scope.Current<IVisitAggregationContext>().TryGet(ctx => ctx.Visit.ContactId), typeof(Guid),
                valueKind: "ContactId",
                calculatedFields: new[]
                {
                    new CalculatedField{Name="Unique visitors", 
                        DaxPattern = string.Format("DISTINCTCOUNT([{0}])", state.AffixName("ContactId")), 
                        ChildDaxPattern = string.Format("CALCULATE(DISTINCTCOUNT(@Parent[{0}]), @TableName)", state.AffixName("ContactId")),
                        FormatString = CalculatedFieldFormat.Integer}
                });
        }
    }
}
