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

namespace ExperienceExtractor.Mapping
{
    public static class MappingHelpers
    {
        public static string Discretize<TValue>(this TValue x, params TValue[] limits)
        {
            var comparer = Comparer<TValue>.Default;

            if (comparer.Compare(x, limits[0]) < 0) return "<" + limits[0];
            for (var i = 1; i < limits.Length; i++)
            {
                if (comparer.Compare(x, limits[i]) < 0) return limits[i - 1] + "-" + limits[i];
            }
            return ">" + limits[limits.Length - 1];
        }

        public static TValue TryGet<TSource, TValue>(this TSource source, Func<TSource, TValue> getter,
            TValue defaultValue = default(TValue))
        {
            if (source == null) return defaultValue;

            var value = getter(source);
            return value != null ? value : defaultValue;
        }        
    }
}