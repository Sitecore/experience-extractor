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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics.Aggregation.Data.Model;

namespace ExperienceExtractor.Components.Parsing.Splitters
{
    [ParseFactory("funnel", "Funnel")]
    public class FunnelFactory : IParseFactory<ITableMapper>
    {
        public ITableMapper Parse(JobParser parser, ParseState state)
        {
            var table = new TableDefinition(state.TryGet("Name", "Funnel"));

            var stepDefinitions = new List<FunnelStepDefinition>();
            var i = 1;
            foreach (var step in state.SelectMany("Steps", true))
            {
                var def = new FunnelStepDefinition { Name = step.TryGet("Name", "Step " + i) };
                def.Events = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var name in step.Require<string[]>("Events"))
                {
                    def.Events.Add(name);
                }
                stepDefinitions.Add(def);
                ++i;
            }

            var helper = new FunnelHelper(stepDefinitions);

            table.FieldMappers = new List<IFieldMapper>();

            table.FieldMappers.Add(
                new SimpleFieldMapper(state.AffixName("StepNumber"), scope => scope.Current<FunnelStep>().Number, typeof(int)));

            table.FieldMappers.Add(
                new SimpleFieldMapper(state.AffixName("Step"), scope => scope.Current<FunnelStep>().Name, typeof(string), sortBy: state.AffixName("StepNumber")));

            table.FieldMappers.Add(
                new SimpleFieldMapper(state.AffixName("Continued"), scope => scope.Current<FunnelStep>().Continued, typeof(int), FieldType.Fact,
                    calculatedFields: new[]
                    {
                        new CalculatedField {Name = state.AffixName("Continued %"), 
                            FormatString = CalculatedFieldFormat.Percentage,
                            DaxPattern = 
                            String.Format("IF(SUM([{0}])=0,Blank(),SUM([{1}])/SUM([{0}]))", state.AffixName("Potential"), state.AffixName("Continued"))}
                    }));

            table.FieldMappers.Add(
                new SimpleFieldMapper(state.AffixName("Fallout"), scope => scope.Current<FunnelStep>().Fallout, typeof(int), FieldType.Fact, 
                    calculatedFields: new[]
                    {
                        new CalculatedField {Name = state.AffixName("Fallout %"), 
                            FormatString = CalculatedFieldFormat.Percentage,
                            DaxPattern = 
                            String.Format("IF(SUM([{0}])=0,Blank(),SUM([{1}])/SUM([{0}]))", state.AffixName("Potential"), state.AffixName("Fallout"))}
                    }));

            table.FieldMappers.Add(
                new SimpleFieldMapper(state.AffixName("Potential"), scope => scope.Current<FunnelStep>().Fallout + scope.Current<FunnelStep>().Continued, typeof(int), FieldType.Fact));

            

            foreach (var fm in state.SelectMany("Fields").Select(parser.ParseFieldMapper))
            {
                table.FieldMappers.Add(fm);
            }
            

            return new SimpleTableMapper(helper.GetSteps, table);
        }

    }

    public class FunnelHelper
    {
        public List<FunnelStepDefinition> Steps { get; set; }

        public FunnelHelper(IEnumerable<FunnelStepDefinition> steps)
        {
            Steps = steps.ToList();
        }

        public IEnumerable<FunnelStep> GetSteps(ProcessingScope scope)
        {
            var ctx = scope.Current<IVisitAggregationContext>();
            if (ctx == null) yield break;

            var currentStep = 0;
            foreach (var ev in ctx.Visit.Pages.SelectMany(p => p.PageEvents))
            {
                if (currentStep >= Steps.Count) break;

                if (Steps[currentStep].Events.Contains(ev.Name) || Steps[currentStep].Events.Contains(ev.PageEventDefinitionId.ToString("D")))
                {
                    yield return new FunnelStep { Number = currentStep + 1, Name = Steps[currentStep].Name, Continued = 1 };
                    ++currentStep;
                }
            }
            if (currentStep < Steps.Count)
            {
                yield return new FunnelStep { Number = currentStep + 1, Name = Steps[currentStep].Name, Fallout = 1 };
            }
        }
    }

    public class FunnelStepDefinition
    {
        public string Name { get; set; }
        public HashSet<string> Events { get; set; }
    }

    public class FunnelStep
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public int Continued { get; set; }
        public int Fallout { get; set; }
    }
}
