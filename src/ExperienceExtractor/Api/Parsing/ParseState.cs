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
using Sitecore.Data.Comparers;

namespace ExperienceExtractor.Api.Parsing
{

    /// <summary>
    /// Represents the current state of a <see cref="JobParser"/> providing access to the current element being parsed.
    /// </summary>
    public abstract class ParseState
    {
        private string _prefix = "";
        private string _postfix = "";

        public JobParser Parser { get; set; }

        public ParseState Parent { get; private set; }
        public string AttributeName { get; private set; }
        
        protected ParseState(string attributeName, JobParser parser, ParseState parent)
        {
            AttributeName = attributeName;
            Parent = parent;

            Parser = parser;

            if (parent != null)
            {
                _prefix = parent._prefix;
                _postfix = parent._postfix;
            }
        }

        /// <summary>
        /// The path of the item being parsed. For example Connection/Xdb/Filters/DateRange
        /// </summary>
        public virtual string Path
        {
            get
            {
                var path = new List<string>();
                var s = this;
                while (s != null)
                {
                    if (!string.IsNullOrEmpty(s.AttributeName))
                    {
                        path.Add(s.AttributeName);
                    }
                    s = s.Parent;
                }

                path.Reverse();
                return string.Join("/", path);
            }
        }

        /// <summary>
        /// Creates an exception to be thrown with information about the position in a job specification
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public ParseException AttributeError(string message, params object[] args)
        {
            return ParseException.AttributeError(this, args.Length > 0 ? string.Format(message, args) : message);
        }

        /// <summary>
        /// Applies the scope's pre and postfixes to a field name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string AffixName(string name)
        {
            if (name == null) return null;
            return _prefix + name + _postfix;
        }

        /// <summary>
        /// Creates a copy of this scope with all inherited pre and postfixes removed
        /// </summary>
        /// <returns></returns>
        public ParseState ClearAffix()
        {
            var child = Clone();
            child._prefix = child._postfix = "";
            return child;
        }

        /// <summary>
        /// Creates a copy of this scope that will add this prefix to field names
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public ParseState Prefix(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;

            var child = Clone();
            child._prefix = text + child._prefix;
            return child;
        }

        /// <summary>
        /// Creates a copy of this scope that will add this postfix to field names
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public ParseState Postfix(string text)
        {
            if (string.IsNullOrEmpty(text)) return this;

            var child = Clone();
            child._postfix += text;

            return child;
        }

        /// <summary>
        /// Creates a clone of this scope
        /// </summary>
        /// <returns></returns>
        public virtual ParseState Clone()
        {
            return (ParseState)MemberwiseClone();
        }


        /// <summary>
        /// Gets the value of an attribute in this scope with the name specified.
        /// </summary>
        /// <typeparam name="TValue">The type of the value</typeparam>
        /// <param name="attribute">The name of the attribute</param>
        /// <param name="defaultValue">A function returning a default value if the attribute is not found in the scope</param>
        /// <param name="mainParameter">If true, the value can be specified with short-hand notation e.g. {"factory": "main parameter value"}</param>
        /// <returns></returns>
        public abstract TValue TryGet<TValue>(string attribute, Func<TValue> defaultValue, bool mainParameter = false);

        /// <summary>
        /// Gets the value of an attribute in this scope with the name specified.
        /// </summary>
        /// <typeparam name="TValue">The type of the value</typeparam>
        /// <param name="attribute">The name of the attribute</param>
        /// <param name="defaultValue">A default value if the attribute is not found in the scope</param>
        /// <param name="mainParameter">If true, the value can be specified with short-hand notation e.g. {"factory": "main parameter value"}</param>
        /// <returns></returns>
        public abstract TValue TryGet<TValue>(string attribute, TValue defaultValue = default(TValue), bool mainParameter = false);

        /// <summary>
        /// The name of the attributes in this scope
        /// </summary>
        public abstract IEnumerable<string> Keys { get; }

        /// <summary>
        /// Gets the value of an attribute in this scope with the name specified, and raises an exception if it is not present.
        /// </summary>
        /// <typeparam name="TValue">The type of the value</typeparam>
        /// <param name="attribute">The name of the attribute</param>
        /// <param name="mainParameter">If true, the value can be specified with short-hand notation e.g. {"factory": "main parameter value"}</param>
        /// <returns></returns>
        public abstract TValue Require<TValue>(string attribute, bool mainParameter = false);

        /// <summary>
        /// Creates a nested scope for the value of the attribute specified
        /// </summary>
        /// <param name="attribute">The name of the attribute</param>
        /// <param name="required">If true, an exception is raised if the attribute is not present</param>
        /// <returns></returns>
        public abstract ParseState Select(string attribute, bool required = false);


        /// <summary>
        /// Creates a nested scope for each of the values of attribute specified assuming it's an array
        /// </summary>
        /// <param name="attribute">The name of the attribute</param>
        /// <param name="required">If true, an exception is raised if the attribute is not present</param>
        /// <returns></returns>
        public abstract IEnumerable<ParseState> SelectMany(string attribute = null, bool required = false);
    }

}