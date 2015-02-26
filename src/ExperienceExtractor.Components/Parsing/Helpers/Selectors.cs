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
using System.Linq.Expressions;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Analytics.Model;
using Sitecore.Globalization;

namespace ExperienceExtractor.Components.Parsing.Helpers
{
    public static class Selectors
    {
        public class GetterInfo
        {
            public string[] Path { get; set; }
            public string SitecoreField { get; set; }
            public Type Type { get; set; }
            public Type ValueType { get; set; }

            public string SuggestedName
            {
                get
                {
                    if (!string.IsNullOrEmpty(SitecoreField))
                    {
                        return SitecoreField.StartsWith("@") ? SitecoreField.Substring(1) : SitecoreField;
                    }
                    else if (Path.Length > 0)
                    {
                        return Path.Last();
                    }
                    return null;
                }
            }

            public object DefaultValue
            {
                get { return ValueType.IsValueType ? Activator.CreateInstance(ValueType) : null; }
            }

            public Func<object, ProcessingScope, object> Getter { get; set; }
        }
        

        public static GetterInfo CompileGetter(this JobParser parser, Type type, string selector, Language language = null)
        {
            var slash = selector.IndexOf('/');
            
            var path = (slash != -1 ? selector.Substring(0, slash) : selector).Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
            var scField = slash != -1 ? selector.Substring(slash + 1) : null;

            Type valueType;
            var valueGetter = CompileGetter(type, path, out valueType);

            Func<object, ProcessingScope, object> getter = (item, scope) => valueGetter(item);
            

            if (!string.IsNullOrEmpty(scField))
            {
                valueType = typeof(string);
                getter =
                    (item, scope) =>
                        valueGetter(item).TryGet(id => scope.FieldLookup.TryGet(lu => lu.Lookup(id, scField, language)));
            }

            return new GetterInfo
            {
                Path = path,
                SitecoreField = scField,
                Type = type,
                ValueType = valueType,
                Getter = getter
            };
        }

        public static Func<object, object> CompileGetter(Type type, string[] path, out Type valueType)
        {
            var arg = Expression.Parameter(typeof(object), "arg");
            var unbox = Expression.Convert(arg, type);
            var getter = CreateGetter(unbox, type, path, out valueType);
            var box = Expression.Convert(getter, typeof(object));

            return Expression.Lambda<Func<object, object>>(box, arg).Compile();
        }

        public static string DefaultSelector(string source)
        {
            source = source.ToLower();
            switch (source)
            {
                case "visits":
                    return "InteractionId";
                case "pages":
                    return "Item.Id";
                case "events":
                case "goals":
                    return "PageEventDefinitionId";
            }
            throw new ArgumentOutOfRangeException("source", "Invalid source");
        }

        public static Func<ProcessingScope, IEnumerable<object>> SelectFromName(string source)
        {
            Type dummy;
            return SelectFromName(source, out dummy);
        }

        public static Func<ProcessingScope, IEnumerable<object>> SelectFromName(string source, out Type itemType)
        {
            source = source.ToLower();
            switch (source)
            {
                case "visits":
                    itemType = typeof (VisitData);
                    return s => s.Current<IVisitAggregationContext>().TryGet(v => new[] {v.Visit}, Enumerable.Empty<VisitData>());

                case "pages":
                    itemType = typeof(PageData);
                    return s => s.Current<IVisitAggregationContext>().TryGet(v => v.Visit.Pages, Enumerable.Empty<PageData>());
                case "events":
                case "goals":
                    itemType = typeof(PageEventData);
                    Func<PageEventData, bool> filter = pe => true;
                    if (source == "goals")
                    {
                        filter = pe => pe.IsGoal;
                    }

                    return s =>
                        (s.Current<PageData>().TryGet(p => new List<PageData> { p }) ??
                         s.Current<IVisitAggregationContext>().TryGet(v => v.Visit.Pages))
                            .TryGet(
                                ps =>
                                    ps.SelectMany(
                                        p =>
                                            p.PageEvents.TryGet(pes => pes.Where(filter),
                                                Enumerable.Empty<PageEventData>())));

            }
            throw new ArgumentOutOfRangeException("source", "Invalid source");
        }

        public static Expression CreateGetter(Expression arg, Type type, string[] selector, out Type valueType)
        {
            var current = arg;
            valueType = current.Type;

            for (var i = 0; i < selector.Length; i++)
            {
                Expression prop = Expression.PropertyOrField(current, selector[i]);
                valueType = prop.Type;

                if (i == selector.Length - 1) prop = Expression.Convert(prop, typeof(object));

                current = Expression.Condition(Expression.Equal(current, Expression.Constant(null)),
                    Expression.Default(prop.Type), prop);
            }

            return current;
        }
    }
}
