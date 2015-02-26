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
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.Globalization;

namespace ExperienceExtractor.Api.Parsing
{
    public class JsonJobParser : JobParser
    {

        private JsonParseState _rootState;


        
        public JsonJobParser(JObject specification, ParseFactories configuration = null)
            : base(configuration)
        {
            _rootState = new JsonParseState(specification, this);
        }

        protected override ParseState RootState
        {
            get { return _rootState; }
        }

        protected override TType Parse<TType>(ParseState state)
        {
            var jsonState = (JsonParseState) state;

            JProperty prop = null;

            var obj = jsonState.Token as JObject;
            if (obj != null)
            {
                prop = obj.First as JProperty;
            }
            if (prop == null)
            {
                var val = jsonState.Token as JValue;
                if (val == null)
                {
                    throw new ArgumentException("Expected string or {'type': {params}} construct");
                }

                prop = new JProperty("" + val.Value, null);
            }

            var factory = Configuration.Get<TType>(prop.Name);

            if (factory == null)
            {
                throw new KeyNotFoundException(string.Format("No parse factory registered for type {0} with key \"{1}\" ({2})", typeof(TType).Name, prop.Name, state.Path));
            }


            return factory.Parse(this, new JsonParseState(prop.Value, this, prop.Name, state));
        }

        public override string ToString()
        {
            var state = (RootState as JsonParseState);
            return state != null ? state.Token.ToString(Formatting.Indented) : "{}";
        }
    }
}
