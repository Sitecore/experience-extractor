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
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing.Helpers;
using ExperienceExtractor.Processing.Keys;

namespace ExperienceExtractor.Processing
{
    /// <summary>
    /// Class for aggregating rows in memory 
    /// Rows are aggregated by key fields, or dimension fields if no key is present
    /// </summary>
    public class TableDataBuilder : TableData
    {
        public IEnumerable<IFieldMapper> FieldMappers { get; set; }

        /// <summary>
        /// A dictionary holding the current rows. Keys and values are the same objects mapped by a <see cref="RowComparer"/> that only selects key/dimension values
        /// </summary>
        public Dictionary<object[], object[]> RowMap { get; private set; }
        

        /// <summary>
        /// The parent table for this table, if any.
        /// </summary>
        public TableDataBuilder Parent { get; private set; }

        /// <summary>
        /// The row that has most recently been added. For child tables this their parent's "current row"
        /// </summary>
        public object[] CurrentRow { get; private set; }

        public override IEnumerable<object[]> Rows
        {
            get { return RowMap.Values.OrderBy(row => row, _rowComparer); }
        }

        public override int? RowCount
        {
            get { return RowMap.Count; }
        }

        public int RowsCreated { get; set; }
        
        

        protected IKeyFactory KeyFactory { get; set; }        
        protected int? HashKey { get; private set; }
        
        protected int[] HashIndices { get; private set; }

        protected FieldMapperIterator Iterator { get; private set; }

        private object[] _emptyRow;
        private readonly List<Field> _fieldList = new List<Field>();        
        private readonly List<KeyValuePair<int, int>> _parentReferenceIndices = new List<KeyValuePair<int, int>>();
        private readonly Dictionary<Field, int> _parentReferences = new Dictionary<Field, int>();
        private Field _hashKeyField;
        private RowComparer _rowComparer;

        public TableDataBuilder(string name, IEnumerable<IFieldMapper> fieldMappers)
            : base(new TableDataSchema(name))
        {
            FieldMappers = fieldMappers.ToArray();
            _fieldList.AddRange(FieldMappers.SelectMany(mapper => mapper.Fields));

            UpdateSchema();
        }


        public bool AddRowFromContext(ProcessingScope context)
        {
            CurrentRow = null;            
            var data = CreateEmptyRow();
                      
            if (SetValues(context, data))
            {
                CurrentRow = AddData(data);
                return true;
            }

            return false;
        }

        protected virtual bool SetValues(ProcessingScope context, object[] data)
        {
            return Iterator.SetValues(data, context);
        }

        public virtual void FinalizeData()
        {            
            Iterator.Apply(Rows, (mapper, rows) => mapper.PostProcessRows(rows));
        }

        public void Clear()
        {
            CurrentRow = null;
            RowMap.Clear();
        }


        public object[] AddData(object[] values)
        {
            if (Parent != null)
            {
                if (Parent.CurrentRow == null)
                {
                    throw new InvalidOperationException("Row cannot be added when parent table has no current row");
                }

                foreach (var pk in _parentReferenceIndices)
                {
                    values[pk.Value] = Parent.CurrentRow[pk.Key];
                }
            }

            if (HashKey.HasValue)
            {
                values[HashKey.Value] = KeyFactory.CalculateKey(HashIndices.Select(ix => values[ix]));
            }


            //Facts with scope unique values (e.g. count only one visit for pages visited multiple times in a visit)            
            foreach (var f in Schema.Facts)
            {
                var defered = values[f.Index] as IDeferedValue;
                if (defered != null)
                {
                    values[f.Index] = defered.GetValue(this, f, values);
                }
            }

            object[] row;
            if (RowMap.TryGetValue(values, out row))
            {
                MergeFacts(row, values);
            }
            else
            {
                RowMap.Add(values, row = values);
                ++RowsCreated;
            }

            return row;
        }

        public void Reset()
        {
            RowMap.Clear();
        }

        public object[] CreateEmptyRow()
        {
            var row = new object[_emptyRow.Length];
            Array.Copy(_emptyRow, row, row.Length);
            return row;
        }


        #region Schema building
        protected void UpdateSchema()
        {
            //Add fields to schema order by field type, then source index
            Schema.Fields =
                _fieldList.AsIndexed()
                .OrderBy(f => f.Value.FieldType == FieldType.Key ? 0 : f.Value.FieldType == FieldType.Fact ? 2 : 1)
                    .ThenBy(f => f.Index)
                    .Select(f => f.Value)
                    .ToArray();



            Schema.CalculatedFields =
                FieldMappers.OfType<ICalculatedFieldContainer>().SelectMany(c => c.CalculatedFields).ToList();

            Iterator = new FieldMapperIterator(FieldMappers, Schema.Fields);

            _rowComparer = new RowComparer(Schema);

            RowMap = new Dictionary<object[], object[]>(_rowComparer);
            
            HashKey = _hashKeyField != null ? Array.IndexOf(Schema.Fields, _hashKeyField) : (int?) null;

            if (HashKey.HasValue)
            {
                HashIndices = Schema.Keys.Concat(Schema.Dimensions).Where(fp=>fp.Index != HashKey).Select(fp => fp.Index).ToArray();
            }

            _parentReferenceIndices.Clear();
            if (_parentReferences.Count > 0)
            {
                foreach (var field in Schema.Fields.AsIndexed())
                {
                    int parentIndex;
                    if (_parentReferences.TryGetValue(field.Value, out parentIndex))
                    {
                        _parentReferenceIndices.Add(new KeyValuePair<int, int>(parentIndex, field.Index));
                    }
                }
            }

            _emptyRow =
                Schema.Fields
                    .Select(
                        f => f.DefaultValue ?? (f.ValueType.IsValueType ? Activator.CreateInstance(f.ValueType) : null))
                    .ToArray();
        }

        public bool EnsureKey(IKeyFactory keyFactory = null)
        {
            if (_fieldList.All(f => f.FieldType != FieldType.Key))
            {
                AddHashKey(keyFactory);
                return true;
            }

            return false;
        }

        public void AddHashKey(IKeyFactory keyFactory = null)
        {
            if( HashKey.HasValue) throw new InvalidOperationException("A hash key has already been added");
            KeyFactory = keyFactory ?? Keys.KeyFactory.Default;            
            
            _fieldList.Add(_hashKeyField = KeyFactory.GetKeyField(Schema));
            
            _hashKeyField.Hide = true;

            UpdateSchema();
        }

        public void LinkParentTable(TableDataBuilder parent)
        {
            if( Parent != null) throw new InvalidOperationException("A parent table builder is already linked");


            var asKey = Schema.Keys.Any();

            Parent = parent;            
            _parentReferences.Clear();
            foreach (var pk in Parent.Schema.Keys)
            {
                _parentReferences.Add(new Field
                {
                    FieldType = asKey ? FieldType.Key : FieldType.Dimension,
                    Name = pk.Value.Name,
                    ValueType = pk.Value.ValueType
                }, pk.Index);
            }
            _fieldList.AddRange(_parentReferences.Keys);

            Schema.Associate(parent.Schema, _parentReferences.Keys, Parent.Schema.Keys.Select(fp=>fp.Value), false);

            UpdateSchema();                                    
        }

        #endregion
    }
}