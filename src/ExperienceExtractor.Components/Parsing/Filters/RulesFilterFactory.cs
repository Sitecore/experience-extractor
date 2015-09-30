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

using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Components.Mapping.Sitecore;
using ExperienceExtractor.Processing.DataSources;
using ExperienceExtractor.Processing.Helpers;
using Sitecore.Analytics.Pipelines.CreateVisits;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Rules;
using Sitecore.SecurityModel;

namespace ExperienceExtractor.Components.Parsing.Filters
{
    [ParseFactory("rule", "Rules filter", "Filters the visits by the rule(s) defined in the item specified"),
        ParseFactoryParameter("Item", typeof(string), "Rule item's ID or path to rule item in Sitecore. If a leading slash is omitted in the path it is relative to /sitecore/system/Marketing Control Panel/Experience Analytics/Filters/")]
    public class RulesFilterFactory : IParseFactory<IDataFilter>
    {
        public IDataFilter Parse(JobParser parser, ParseState state)
        {
            using (new SecurityDisabler())
            {
                var ruleContextItem = parser.Database.GetRootItem(parser.DefaultLanguage);

                var json = state.TryGet<string>("Rule");
                if (!string.IsNullOrEmpty(json))
                {
                    var filter = RulesFilter.FromString(json);
                    filter.RuleContextItem = ruleContextItem;
                    return filter;
                }


                var ruleItem = state.Require<string>("Item", true);
                Item item;
                ID id;
                if (!ID.TryParse(ruleItem, out id))
                {
                    if (!ruleItem.StartsWith("/"))
                    {
                        var rootItem =
                            state.Parser.Database.GetItem(
                                ExperienceExtractorApiContainer.ItemPaths.GetOrDefault("experienceAnalyticsFilters") ??
                                "/sitecore/system/Marketing Control Panel/Experience Analytics/Filters");

                        ruleItem = rootItem.Paths.FullPath + "/" + ruleItem;
                    }


                    item = parser.Database.GetItem(ruleItem, parser.DefaultLanguage);
                }
                else
                {
                    item = parser.Database.GetItem(id, parser.DefaultLanguage);
                }



                if (item == null)
                {
                    throw ParseException.AttributeError(state, string.Format("Rule item not found '{0}'", ruleItem));
                }

                var rules = RuleFactory.GetRules<RuleContext>(item.Fields["Rule"]);

                return new RulesFilter(rules)
                {
                    RuleContextItem = ruleContextItem
                };
            }
        }
    }
}
