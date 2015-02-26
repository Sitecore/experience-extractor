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
    public class ParseException : ApplicationException
    {
        public ParseState State { get; set; }

        public ParseException(ParseState state, string message)
            : base(message)
        {
            State = state;
        }

        public static ParseException MissingAttribute(ParseState state, string name)
        {
            return new ParseException(state, string.Format("{0} was expected while parsing {1}", name, state.Path));
        }

        public static ParseException AttributeError(ParseState state, string description)
        {
            return new ParseException(state, description + string.Format(" while parsing {0}", state.Path));
        }
    }
}