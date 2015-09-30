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
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Data.Schema;
using Sitecore.Analytics.Aggregation.Data.Model;
using Sitecore.Common;

namespace ExperienceExtractor.Components.PostProcessors
{
    public static class SqlUpdateUtil
    {
        public static string GetJoinToFactAncestor(TableDataSchema schema, string ancestorCriteria)
        {
            var sb = new StringBuilder();
            GetJoinToFactAncestor(schema, ancestorCriteria, sb);
            return sb.ToString();
        }

        public static void GetJoinToFactAncestor(TableDataSchema schema, string ancestorCriteria, StringBuilder sb)
        {
            var parent = schema.RelatedTables.FirstOrDefault(r => r.RelationType == RelationType.Parent);
            sb.AppendLine();
            if (parent == null)
            {
                sb.Append("WHERE ").Append(ancestorCriteria);
            }
            else
            {
                sb.AppendFormat("INNER JOIN [{0}] ON ", parent.RelatedTable.Name);
                for (var i = 0; i < parent.Fields.Count; i++)
                {
                    if (i > 0) sb.Append(" AND ");
                    sb.AppendFormat("[{0}].[{1}] = [{2}].[{3}]",
                        parent.RelatedTable.Name, parent.RelatedFields[i].Name,
                        schema.Name, parent.Fields[i].Name);
                }

                GetJoinToFactAncestor(parent.RelatedTable, ancestorCriteria, sb);
            }
        }

        public static DateTime GetPartitionDate(DateTime date, TimeSpan lag)
        {
            if (lag == TimeSpan.Zero) return date;

            return new DateTime(date.Ticks / lag.Ticks * lag.Ticks);
        }

        public static Tuple<TableDataSchema, PartitionField> GetPartitionField(TableDataSchema schema)
        {
            if (schema.IsCenterTable())
            {
                var field = schema.Fields.OfType<PartitionField>().FirstOrDefault();
                return field != null ? new Tuple<TableDataSchema, PartitionField>(schema, field) : null;
            }

            var parent = schema.RelatedTables.FirstOrDefault(r => r.RelationType == RelationType.Parent);
            return parent != null ? GetPartitionField(parent.RelatedTable) : null;
        }

        public static string GetUpdateCriteria(TableDataSchema table, Tuple<TableDataSchema, PartitionField> partitionField, bool stale, DateTime? date = null, DateTime? cutoff = null)
        {
            var field = string.Format("[{0}].[{1}]", partitionField.Item1.Name, partitionField.Item2.Name);
            var refDate = GetPartitionDate(date ?? DateTime.UtcNow.Add(-partitionField.Item2.StaleTime), partitionField.Item2.StaleTime);

            var criteria = new StringBuilder(field);
            criteria.Append(stale ? ">=" : "<");
            criteria.AppendFormat("'{0}'", refDate.ToString("o"));

            if (cutoff.HasValue && !stale)
            {
                criteria.AppendFormat(" AND {0} >= '{1}'", field, cutoff.Value.ToString("o"));
            }
            
            return GetJoinToFactAncestor(table, criteria.ToString());
        }
    }
}
