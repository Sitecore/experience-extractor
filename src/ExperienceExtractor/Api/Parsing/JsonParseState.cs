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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Mapping;

namespace ExperienceExtractor.Api.Parsing
{
    public class JsonParseState : ParseState
    {
        public JToken Token { get; private set; }

        public JsonParseState(JToken token, JobParser parser, string attributeName = null, ParseState parent = null)
            : base(attributeName, parser, parent)
        {
            Token = token;
        }

        private JToken GetProperty(string property = null)
        {
            if (property == null) return Token.Type == JTokenType.Null ? null : Token;

            var obj = Token as JObject;
            if (obj != null)
            {
                var value = obj.GetValue(property, StringComparison.InvariantCultureIgnoreCase);
                if (value != null && value.Type != JTokenType.Null)
                {
                    return value;
                }
            }
            return null;
        }


        public override TValue TryGet<TValue>(string attribute, Func<TValue> defaultValue, bool mainParameter = false)
        {
            if (typeof (TValue).IsEnum)
            {
                var stringValue = TryGet<string>(attribute);
                if (stringValue == null)
                {
                    return defaultValue();
                }

                return ParseEnum<TValue>(stringValue);
            }

            var val = GetProperty(attribute);
            if (attribute == null && val is JObject)
            {
                val = null;
            }

            if (val == null && attribute != null && mainParameter)
            {
                return TryGet(null, defaultValue);
            }

            return val == null ? defaultValue() : val.ToObject<TValue>();
        }

        public override TValue TryGet<TValue>(string attribute, TValue defaultValue = default(TValue), bool mainParameter = false)
        {
            return TryGet(attribute, () => defaultValue, mainParameter: mainParameter);
        }

        public override IEnumerable<string> Keys
        {
            get
            {
                var o = Token as JObject;
                if (o != null)
                {
                    foreach (var prop in o.OfType<JProperty>())
                    {
                        yield return prop.Name;
                    }
                }
            }
        }

        public override TValue Require<TValue>(string attribute, bool mainParameter = false)
        {
            var prop = GetProperty(attribute);
            if (prop == null && mainParameter)
            {
                prop = GetProperty(null);
            }

            if (prop == null) throw ParseException.MissingAttribute(this, attribute);            

            return TryGet<TValue>(attribute, mainParameter: mainParameter);
        }
        
        public override ParseState Select(string attribute, bool required = false)
        {
            var value = GetProperty(attribute);
            if (value != null)
            {
                return new JsonParseState(value, Parser, attribute, this);
            }

            if (required)
            {
                throw ParseException.MissingAttribute(this, attribute);
            }

            return null;
        }

        public TEnum ParseEnum<TEnum>(string value)
        {
            try
            {
                return (TEnum) Enum.Parse(typeof (TEnum), value, true);
            }
            catch
            {
                throw ParseException.AttributeError(this, "Invalid value specified for " + typeof(TEnum).Name + " (" + value + ")");
            }                                    
        }


        public override IEnumerable<ParseState> SelectMany(string attribute = null, bool required = false)
        {
            var prop = GetProperty(attribute);

            var value = prop is JObject ? new JArray(prop) : prop as JArray;

            if (value != null)
            {
                foreach (var item in value)
                {
                    if (item.Type != JTokenType.Comment)
                    {
                        yield return new JsonParseState(item, Parser, attribute, this);
                    }
                }
                yield break;
            }

            if (required)
            {
                throw ParseException.MissingAttribute(this, attribute);
            }

        }
    }
}
