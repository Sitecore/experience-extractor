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

using ExperienceExtractor.Mapping;
using Sitecore.Shell.Applications.ContentEditor;
using DateTime = System.DateTime;

namespace ExperienceExtractor.Processing.Keys
{
    public static class KeyFactory
    {
        private static IKeyFactory _default = new Fnv1a64();

        public static IKeyFactory Default
        {
            get { return _default; }
            set { _default = value; }
        }


        //public static long ToKeyOrder(object orderValue, SortOrder sortOrder = SortOrder.Ascending)
        //{
        //    if (orderValue is DateTime)
        //    {
        //        return ((DateTime) orderValue).Ticks;
        //    }

        //    return orderValue != null ? System.Convert.ToInt64(orderValue) : 0L;
        //}

        //public static long GetHash64(params object[] values)
        //{
        //    return GetHash64((IEnumerable<object>)values);
        //}

        ////public static long GetHash64(IEnumerable<object> values)
        ////{
        ////    unchecked
        ////    {
        ////        var hash = 17L;
        ////        foreach (var o in values)
        ////        {
        ////            var h = o == null ? 0 : o.GetHashCode();
        ////            hash = (hash << 5) + (h ^ hash);
        ////        }
        ////        return hash;
        ////    }
        ////}

        //public static long GetHash64(IEnumerable<object> values)
        //{
        //    unchecked
        //    {
        //        const long fnv64Prime = 0x100000001b3;
        //        var hash = (long)14695981039346656037;
        //        foreach (var o in values)
        //        {
        //            hash = (hash ^ (o == null ? 0 : o.GetHashCode())) * fnv64Prime;
        //        }

        //        return hash;
        //    }
        //}
    }
}