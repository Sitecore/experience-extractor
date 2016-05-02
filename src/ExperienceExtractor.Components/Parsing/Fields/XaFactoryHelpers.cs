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
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Processing.Helpers;
using Sitecore.Data.Items;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;
using Sitecore.ExperienceAnalytics.Api;
using Sitecore.SecurityModel;

namespace ExperienceExtractor.Components.Parsing.Fields
{
    public static class XaFactoryHelpers
    {

        public static IDimension ParseDimension(this ParseState state)
        {

            using (new SecurityDisabler())
            {
                var dimensionString = state.Require<string>("Dimension", true);


                Guid dimensionId;
                if (Guid.TryParse(dimensionString, out dimensionId))
                {
                    var dim = ApiContainer.Repositories.GetDimensionDefinitionService().GetDimension(dimensionId);

                    if (dim == null)
                    {
                        throw ParseException.AttributeError(state,
                            string.Format("Dimension '{0}' is not registered", dimensionId));
                    }
                    return dim;
                }

                if (state.Parser.Database != null)
                {
                    var path = dimensionString;
                    if (!path.StartsWith("/"))
                    {
                        var rootItem =
                            state.Parser.Database.GetItem(
                                ExperienceExtractorApiContainer.ItemPaths.GetOrDefault("experienceAnalyticsDimensions") ??
                                "/sitecore/system/Marketing Control Panel/Experience Analytics/Dimensions",
                                state.Parser.DefaultLanguage);

                        path = rootItem.Paths.FullPath + "/" + path;
                    }

                    var item = state.Parser.Database.GetItem(path, state.Parser.DefaultLanguage);
                    if (item != null)
                    {
                        var dim = ApiContainer.Repositories.GetDimensionDefinitionService().GetDimension(item.ID.Guid);
                        if (dim == null)
                        {
                            throw ParseException.AttributeError(state,
                                string.Format("Dimension '{0}' ({1}) is not registered", dimensionId, path));
                        }
                        return dim;
                    }
                }

                throw ParseException.AttributeError(state,
                    string.Format("Dimension item not found \"{0}\"", dimensionString));
            }
        }      
  
    }
}
