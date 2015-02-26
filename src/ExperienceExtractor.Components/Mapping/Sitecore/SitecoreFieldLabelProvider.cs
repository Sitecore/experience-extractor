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

using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Globalization;

namespace ExperienceExtractor.Components.Mapping.Sitecore
{
    public class SitecoreFieldLabelProvider : ILabelProvider
    {
        public IItemFieldLookup Lookup { get; private set; }
        public string Path { get; set; }
        public Language Language { get; set; }

        public SitecoreFieldLabelProvider(string path, Language language)
        {            
            Path = path;
            Language = language;
        }

        public void Initialize(DataProcessor processor)
        {
            Lookup = processor.FieldLookup;
        }

        public string GetLabel(object key)
        {
            return Lookup != null ? Lookup.Lookup(key, Path, Language) : null;
        }
    }
}
