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
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Diagnostics;
using Sitecore.ExperienceAnalytics.Aggregation.Data.Model;
using Sitecore.ExperienceAnalytics.Api;
using Sitecore.ExperienceAnalytics.Api.Response.DimensionKeyTransformers;
using Sitecore.Globalization;

namespace ExperienceExtractor.Components.Mapping.Sitecore
{
    public class XaLabelProvider : ILabelProvider
    {
        public IDimension Dimension { get; set; }
        public IDimensionKeyTransformer KeyTransformer { get; set; }
        public Language Language { get; set; }

        public XaLabelProvider(IDimensionKeyTransformer keyTransformer, Language language)
        {
            KeyTransformer = keyTransformer;
            Language = language;
        }


        public void Initialize(DataProcessor processor)
        {
            
        }

        public string GetLabel(object key)
        {
            if (KeyTransformer == null || key == null)
            {
                return null;
            }

            try
            {
                var trans = KeyTransformer.Transform(key as string, Language);
                if (string.IsNullOrEmpty(trans)) trans = string.Format("{0}({1})",
                    string.IsNullOrEmpty(KeyTransformer.UnknownLabel) ? "" : KeyTransformer.UnknownLabel + " ", key);
                return trans;
            }
            catch (Exception ex)
            {
                Log.SingleError(string.Format("Error loading label {0} ({1})", key, ex), this);
                return "(Error: " + key + ")";
            }
        }

        public static XaLabelProvider FromDimension(IDimension dimension, Language language)
        {
            try
            {
                var keyTransformer = dimension as IDimensionKeyTransformer ??
                                     ApiContainer.Repositories.GetDimensionDefinitionService().GetDimensionKeyTransformer(dimension.DimensionId);
                return keyTransformer != null ? new XaLabelProvider(keyTransformer, language) : null;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting dimension key transformer for dimension " + dimension.DimensionId, ex, typeof(XaLabelProvider));
                return null;
            }
        }        
    }
}
