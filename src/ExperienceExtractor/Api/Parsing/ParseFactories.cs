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
using Sitecore.Diagnostics;

namespace ExperienceExtractor.Api.Parsing
{
    public class ParseFactories
    {
        private readonly Dictionary<Type, Dictionary<string, IParseFactory>> _factories
            = new Dictionary<Type, Dictionary<string, IParseFactory>>();

        public virtual IParseFactory<TType> Get<TType>(string key)
        {
            var factory = (IParseFactory<TType>)null;

            Dictionary<string, IParseFactory> typeFactories;
            if (_factories.TryGetValue(typeof(TType), out typeFactories))
            {
                IParseFactory factoryObject;
                if (typeFactories.TryGetValue(key, out factoryObject))
                {
                    factory = factoryObject as IParseFactory<TType>;
                }
            }

            return factory;
        }


        public void Register<TType>(string key, IParseFactory<TType> factory)
        {
            GetTypeFactories(typeof(TType))[key] = factory;
        }



        public IEnumerable<KeyValuePair<string, IParseFactory<TType>>> GetFactories<TType>()
        {
            var list = GetTypeFactories(typeof (TType));
            if (list != null)
            {
                foreach (var factory in list)
                {
                    yield return new KeyValuePair<string, IParseFactory<TType>>(factory.Key, (IParseFactory<TType>) factory.Value);
                }
            }
        }
        
        Dictionary<string, IParseFactory> GetTypeFactories(Type type)
        {
            Dictionary<string, IParseFactory> typeFactories;
            if (!_factories.TryGetValue(type, out typeFactories))
            {
                _factories.Add(type, typeFactories = new Dictionary<string, IParseFactory>(StringComparer.InvariantCultureIgnoreCase));
            }
            return typeFactories;
        }

        public ParseFactories InitializeFromAttributes(Assembly assembly)
        {

            foreach (var type in assembly.GetAllTypes())
            {
                foreach (var factoryAttr in
                    type.GetCustomAttributes(typeof(ParseFactoryAttribute), true)
                        .Cast<ParseFactoryAttribute>())
                {
                    foreach (
                        var factoryType in
                            type.GetInterfaces()
                                .Where(
                                    i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IParseFactory<>))
                                .Select(i => i.GetGenericArguments()[0]))
                    {

                        var typeFactories = GetTypeFactories(factoryType);
                        IParseFactory current;
                        if (typeFactories.TryGetValue(factoryAttr.Key, out current))
                        {
                            if (current.GetType() != type)
                            {
                                Log.Warn(
                                    string.Format(
                                        "{2} ignored. Another factory {0} is already registered for the key {1}",
                                        current.GetType().FullName, factoryAttr.Key, type.FullName), this);
                            }
                        }
                        else
                        {
                            typeFactories.Add(factoryAttr.Key, (IParseFactory) Activator.CreateInstance(type));
                        }
                    }
                }
            }

            return this;
        }

        private static ParseFactories _defaultInstance;
        public static ParseFactories Default
        {
            get
            {
                if (_defaultInstance == null)
                {
                    lock (typeof(JsonJobParser))
                    {
                        if (_defaultInstance == null)
                        {
                            _defaultInstance = new ParseFactories().InitializeFromAttributes(typeof(ParseFactories).Assembly);
                        }
                    }
                }

                return _defaultInstance;
            }
        }
    }
}
