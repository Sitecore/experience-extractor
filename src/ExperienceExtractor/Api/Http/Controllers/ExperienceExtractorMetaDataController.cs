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
using System.Reflection;
using System.Web.Http;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Mapping.Splitting;
using ExperienceExtractor.Processing.DataSources;

namespace ExperienceExtractor.Api.Http.Controllers
{

    [RequireSitecoreLogin]
    public class ExperienceExtractorMetaDataController : ApiController
    {

        [HttpGet]
        public IEnumerable<ParseFactoryInfo> ParseFactories()
        {
            return GetParseFactories<IDataSource>()
                .Concat(GetParseFactories<IDataFilter>())
                .Concat(GetParseFactories<IFieldMapper>())
                .Concat(GetParseFactories<ITableMapper>())
                .Concat(GetParseFactories<ISplitter>())
                .Concat(GetParseFactories<TableDefinition>());
        }


        static IEnumerable<ParseFactoryInfo> GetParseFactories<TType>()
        {
            return Parsing.ParseFactories.Default.GetFactories<TType>()
                .OrderBy(f => f.Key)
                .Select(f => ParseFactoryInfo.FromType(f.Value.GetType(), typeof(TType).Name));
        } 
    }

    public class ParseFactoryInfo
    {
        public string Assembly { get; set; }
        public string Key { get; set; }
        public string FactoryType { get; set; }

        public string Application { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public List<ParseFactoryParameterInfo> Parameters { get; set; }

        public ParseFactoryInfo()
        {
            Parameters = new List<ParseFactoryParameterInfo>();
        }


        public static ParseFactoryInfo FromType(Type type, string application)
        {
            var attr = type.GetCustomAttribute<ParseFactoryAttribute>();

            
            return new ParseFactoryInfo
            {
                Assembly = type.Assembly.GetName().Name,
                FactoryType = type.FullName,
                Key = attr.Key,
                Application = application,
                Name = attr.Name,
                Description = attr.Description,
                Parameters = type.GetCustomAttributes<ParseFactoryParameterAttribute>().Select(
                    ParseFactoryParameterInfo.FromAttribute).Reverse().ToList()
            };
        }
    }

    public class ParseFactoryParameterInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string DefaultValue { get; set; }
        public bool isMainParameter { get; set; }

        public ParseFactoryParameterInfo()
        {
            
        }

        public static ParseFactoryParameterInfo FromAttribute(ParseFactoryParameterAttribute attr)
        {
            return new ParseFactoryParameterInfo
            {
                DefaultValue = attr.DefaultValue,
                Description = attr.Description,
                isMainParameter = attr.IsMainParameter,
                Name = attr.Name,
                Type = GenericTypeToFriendlyName(attr.Type)
            };
        }

        static string GenericTypeToFriendlyName(Type t)
        {
            if (t.IsGenericType)
            {
                return t.GetGenericTypeDefinition().Name.Split('`')[0] + "<" +
                       string.Join(", ", t.GetGenericArguments().Select(GenericTypeToFriendlyName)) + ">";
            }

            return t.Name;
        }
    }
}
