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

namespace ExperienceExtractor.Api.Parsing
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ParseFactoryParameterAttribute : Attribute
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
        public bool IsMainParameter { get; set; }
        public bool Required { get; set; }

        public ParseFactoryParameterAttribute(string name, Type type, string description,             
            string defaultValue = null, bool isMainParameter = false, bool required = false)
        {
            Name = name;
            Type = type;
            Description = description;
            DefaultValue = defaultValue;
            IsMainParameter = isMainParameter;
            Required = required;
        }
    }
}
