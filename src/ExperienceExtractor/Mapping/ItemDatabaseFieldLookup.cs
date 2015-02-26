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
using ExperienceExtractor.Processing.Labels;
using Sitecore.Data;
using Sitecore.Globalization;

namespace ExperienceExtractor.Mapping
{
    public class ItemDatabaseFieldLookup : IItemFieldLookup
    {
        public Database Database { get; set; }
        public Language Language { get; set; }

        private readonly LruCache<string, string> _cache; 
        public ItemDatabaseFieldLookup(Database database, Language language, int cacheSize = 10000)
        {
            Database = database;
            Language = language;
            _cache = new LruCache<string, string>(cacheSize);
        }

        public virtual string Lookup(object itemId, string path, Language language)
        {
            var id = ToGuid(itemId);
            if (Database == null || id == null || path == null)
            {
                return null;
            }

            language = language ?? Language;
            
            var key = string.Concat(language.Name, id, path);
            return _cache.GetOrAdd(key, _ =>
            {
                var item = Database.GetItem(ID.Parse(id), language);
                if (item != null)
                {                    
                    if (path.Equals("@displayname", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return item.DisplayName;
                    }
                    if (path.Equals("@templatename", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return item.TemplateName;
                    }                    

                    return item[path];
                }
                return null;
            });
        }

        static Guid? ToGuid(object value)
        {
            if (value == null) return null;

            if (value is Guid) return (Guid)value;

            var s = value as string;
            Guid g;
            return s != null && Guid.TryParse(s, out g) ? (Guid?)g : null;
        }

    }
}
