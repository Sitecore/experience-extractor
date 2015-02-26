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

using System.Collections.Generic;
using System.Linq;
using ExperienceExtractor.Processing.Helpers;

namespace ExperienceExtractor.Data.Schema
{
    /// <summary>
    /// Defines the schema of a table
    /// </summary>
    public class TableDataSchema
    {
        /// <summary>
        /// The name of the table
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The key fields in this table with their index in the Fields list.
        /// </summary>
        public Indexed<Field>[] Keys { get; private set; }

        /// <summary>
        /// The label fields in this table with their index in the Fields list.
        /// </summary>        
        public Indexed<Field>[] Labels { get; private set; }

        /// <summary>
        /// The dimension fields of this table with their index in the Fields list.
        /// </summary>        
        public Indexed<Field>[] Dimensions { get; private set; }

        /// <summary>
        /// The fact fields of this table with their index in the Fields list.
        /// </summary>        
        public Indexed<Field>[] Facts { get; private set; }

        private Field[] _fields;

        /// <summary>
        /// The fields of this table
        /// </summary>
        public Field[] Fields
        {
            get { return _fields; }
            set
            {
                _fields = value;
                Keys = _fields.FindIndices(f => f.FieldType == FieldType.Key).ToArray();
                Labels = _fields.FindIndices(f => f.FieldType == FieldType.Label).ToArray();
                Dimensions = _fields.FindIndices(f => f.FieldType == FieldType.Dimension).ToArray();
                Facts = _fields.FindIndices(f => f.FieldType == FieldType.Fact).ToArray();
            }
        }

        /// <summary>
        /// Related tables
        /// </summary>
        public List<TableDataRelation> RelatedTables { get; set; }

        public TableDataSchema(string name)
        {
            Name = name;
            _fields = new Field[0];
            RelatedTables = new List<TableDataRelation>();
        }


        /// <summary>
        /// Associates another table with this table in the way specified
        /// </summary>
        /// <param name="referencedTable">The table to reference</param>
        /// <param name="fields">The foreign key fields</param>
        /// <param name="referencedFields">The primary key fields</param>
        /// <param name="dimensionTable">true if the referenced table is a dimension table (0..1). Otherwise, a parent/child relationship is defined (1..1)</param>
        public void Associate(TableDataSchema referencedTable, IEnumerable<Field> fields, IEnumerable<Field> referencedFields, bool dimensionTable = false)
        {
            RelatedTables.Add(new TableDataRelation
            {
                Fields = fields.ToArray(),
                RelationType = dimensionTable ? RelationType.Dimension : RelationType.Parent,
                RelatedFields = referencedFields.ToArray(),
                RelatedTable = referencedTable
            });

            referencedTable.RelatedTables.Add(new TableDataRelation
            {
                Fields = referencedFields.ToArray(),
                RelationType = dimensionTable ? RelationType.DimensionReference : RelationType.Child,
                RelatedFields = fields.ToArray(),
                RelatedTable = this
            });
        }

        /// <summary>
        /// Removes all relations from this table and the relations' counterparts in related tables
        /// </summary>
        public void ClearRelations()
        {
            foreach (var reference in RelatedTables)
            {
                reference.RelatedTable.RelatedTables.RemoveAll(other => other.RelatedTable == this);
            }
            RelatedTables.Clear();            
        }

        /// <summary>
        /// Returns true if the fields in this schema are equivalent to the fields in the other table (i.e. the two tables can contain the same data)
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool FieldsAreEqual(TableDataSchema other)
        {
            return Fields.SequenceEqual(other.Fields);
        }        
    }
}
