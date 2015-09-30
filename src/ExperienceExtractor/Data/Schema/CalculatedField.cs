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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExperienceExtractor.Data.Schema
{
    /// <summary>
    /// A calculated field for SSAS tabular defined by a DAX expression
    /// </summary>
    public class CalculatedField
    {
        public string Name { get; set; }

        public string DaxPattern { get; set; }

        /// <summary>
        /// Dax expression to include in child tables
        /// </summary>
        public string ChildDaxPattern { get; set; }

        public string FormatString { get; set; }

        private static Regex _refParser = new Regex(@"\@Parent\[(?<ValueKind>[^\]]+)\]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string FormatDax(string daxPattern, TableDataSchema schema)
        {
            //TODO: Parents at higher levels
            var allReferencesFound = true;
            var dax = _refParser.Replace(daxPattern.Replace("@TableName", "'" + schema.Name + "'"), match =>
            {
                var parent =
                    schema.RelatedTables.Where(r => r.RelationType == RelationType.Parent)
                        .Select(r => r.RelatedTable)
                        .FirstOrDefault();
                if (parent != null)
                {
                    var refField = parent.Fields.FirstOrDefault(f => f.ValueKind == match.Groups["ValueKind"].Value) ??
                                   parent.Fields.FirstOrDefault(f => f.Name == match.Groups["ValueKind"].Value);
                    if (refField != null)
                    {
                        return string.Format("'{0}'[{1}]", parent.Name, refField.Name);
                    }
                }

                allReferencesFound = false;
                return "";
            });

            return allReferencesFound ? dax : null;
        }
    }

    public static class CalculatedFieldFormat
    {
        public const string Percentage = "'#,0.00 %;-#,0.00 %;#,0.00 %'";
        public const string Integer = "'#,0'";
        public const string Decimal = "'#,0.00'";
    }
}
