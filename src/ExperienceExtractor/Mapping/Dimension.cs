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
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.Keys;

namespace ExperienceExtractor.Mapping
{
    public class Dimension : FieldMapperBase
    {
        public bool InlineFields { get; protected set; }

        public string FieldNamePrefix { get; set; }
        public string TableName { get; set; }

        public bool Key { get; set; }

        public IKeyFactory HashKeyFactory { get; set; }
        
        public Func<string, string> ReferenceNameFormatter { get; set; }

        protected List<IFieldMapper> FieldMappers { get; private set; }

        protected Dimension(string fieldNamePrefix, string tableName, IEnumerable<IFieldMapper> fieldMappers)
        {
            FieldNamePrefix = fieldNamePrefix;
            TableName = tableName;
            HashKeyFactory = KeyFactory.Default;
            FieldMappers = fieldMappers.ToList();
        }

        protected virtual TableDataBuilder CreateLookupBuilder()
        {
            return new TableDataBuilder(TableName, FieldMappers);                       
        }
        
        protected TableDataBuilder LookupTableBuilder { get; private set; }
        private FieldMapperIterator _iterator;
        
        protected override IEnumerable<Field> CreateFields()
        {
            _iterator = new FieldMapperIterator(FieldMappers);

            if (!InlineFields)
            {
                LookupTableBuilder = CreateLookupBuilder();
                if (LookupTableBuilder.Schema.Keys.Length == 0)
                {
                    LookupTableBuilder.AddHashKey(HashKeyFactory);
                }
            }

            return GetFields(InlineFields ? FieldTarget.Inline : FieldTarget.KeyReference).ToArray(); 
        }

        public override void Initialize(DataProcessor processor)
        {
            //Initialize realted tables in field mappers
            foreach (var fm in FieldMappers)
            {
                fm.Initialize(processor);
            }
            
            base.Initialize(processor);
        }

        public override void InitializeRelatedTables(DataProcessor processor, TableDataBuilder table)
        {
            if (!InlineFields)
            {
                var tables = processor.TableMap.Tables;
                var current = tables.FirstOrDefault(t => t.Name == TableName) as TableDataBuilder;

                if (current != null)
                {
                    if (!current.Schema.FieldsAreEqual(LookupTableBuilder.Schema) || current.GetType() != LookupTableBuilder.GetType())
                    {
                        throw new InvalidOperationException("Lookup tables with the same name must have the same schema and type to be shared");
                    }
                    LookupTableBuilder.Schema.ClearRelations();
                    LookupTableBuilder = current;
                }
                else
                {
                    //Insert dimension tables in the start, since it is referenced by other tables
                    tables.Insert(0, LookupTableBuilder);
                }

                table.Schema.Associate(LookupTableBuilder.Schema,
                    table.Schema.Fields.Where(f => Fields.Contains(f)).ToArray(),
                    LookupTableBuilder.Schema.Keys.Select(fp => fp.Value), true);
            }

            //Initialize realted tables in field mappers
            foreach (var fm in FieldMappers)
            {
                fm.InitializeRelatedTables(processor, InlineFields ? table : LookupTableBuilder);
            }

            base.InitializeRelatedTables(processor, table);
        }

        public override bool SetValues(ProcessingScope scope, IList<object> row)
        {
            if (InlineFields)
            {
                return SetValues(FieldTarget.Inline, scope, row);
            }
            else
            {
                if (!LookupTableBuilder.AddRowFromContext(scope))
                {
                    return false;
                }

                return SetValues(FieldTarget.KeyReference, scope, row);
            }
        }

        public override void PostProcessRows(IEnumerable<IList<object>> rows)
        {
            if (InlineFields)
            {
                _iterator.Apply(rows, (mapper, mapperRows) => mapper.PostProcessRows(mapperRows));
            }
        }

        protected bool SetValues(FieldTarget target, ProcessingScope context, IList<object> row)
        {
            switch (target)
            {
                case FieldTarget.Inline:
                    return _iterator.SetValues(row, context);
                
                case FieldTarget.KeyReference:
                    
                    var i = 0;
                    foreach(var key in LookupTableBuilder.Schema.Keys)
                    {
                        row[i] = LookupTableBuilder.CurrentRow[key.Index];
                        ++i;
                    }
                    return true;
            }

            throw new ArgumentOutOfRangeException("target");
        }

        protected virtual string PrefixName(string name)
        {
            return name.StartsWith(FieldNamePrefix) ? name : FieldNamePrefix + name;
        }

        
        protected virtual IEnumerable<Field> GetFields(FieldTarget target)
        {            
            switch (target)
            {
                case FieldTarget.KeyReference:

                    return LookupTableBuilder.Schema.Keys.Select(fp =>
                        fp.Value.Affix(PrefixName).Affix(ReferenceNameFormatter).AsNullableReference(Key ? FieldType.Key : FieldType.Dimension).ChangeSort(SortOrder.Unspecified));


                case FieldTarget.Inline:

                    var fields = FieldMappers.SelectMany(mapper => mapper.Fields.Select(field =>
                        field.Affix(PrefixName).Affix(ReferenceNameFormatter))).ToArray();

                    if (!Key)
                    {
                        foreach (var field in fields.Where(f=>f.FieldType==FieldType.Key))
                        {
                            field.FieldType = FieldType.Dimension;
                        }
                    }

                    return fields;

                case FieldTarget.Table:
                    return FieldMappers.SelectMany(mapper => mapper.Fields);
            }

            throw new ArgumentOutOfRangeException("target");
        }        
        
        protected enum FieldTarget
        {
            Inline,
            KeyReference,
            Table
        }        
    }
}