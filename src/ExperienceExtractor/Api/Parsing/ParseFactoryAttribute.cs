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

namespace ExperienceExtractor.Api.Parsing
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ParseFactoryAttribute : Attribute
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public ParseFactoryAttribute(string key, string name, string description = null)
        {
            Key = key;
            Name = name;
            Description = description;
        }
    }
}
