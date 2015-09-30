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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExperienceExtractor.Data.Schema;
using Sitecore.Analytics.Model;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.Keys;

namespace ExperienceExtractor.Mapping
{
    public abstract class TableMapperBase : ITableMapper
    {
        protected List<TableDefinition> TableDefinitions { get; set; }

        protected TableMapperBase(params TableDefinition[] definitions)
        {
            TableDefinitions = definitions.ToList();
        }


        protected List<BuilderInfo> Builders { get; set; }

        public virtual void Initialize(DataProcessor processor, TableDataBuilder parentTable)
        {
            Builders = new List<BuilderInfo>();
            foreach (var def in TableDefinitions)
            {
                var fieldMappers = def.FieldMappers.ToArray();
                foreach (var mapper in fieldMappers)
                {
                    mapper.Initialize(processor);
                }


                var builder = fieldMappers.Length == 0
                    ? null
                    : new TableDataBuilder(def.Name, fieldMappers);                

                var builderInfo = new BuilderInfo(builder);

                if( builder != null )                
                {
                    if (parentTable != null)
                    {
                        builder.LinkParentTable(parentTable);
                    }

                    builder.EnsureKey(KeyFactory.Default);

                    processor.TableMap.Tables.Add(builderInfo.Builder);
                }

                foreach (var fieldMapper in fieldMappers)
                {
                    fieldMapper.InitializeRelatedTables(processor, builderInfo.Builder);
                }

                if (def.TableMappers != null)
                {
                    foreach (var childTable in def.TableMappers)
                    {
                        builderInfo.Children.Add(childTable);
                        childTable.Initialize(processor, builder ?? parentTable);
                    }
                }

                Builders.Add(builderInfo);
            }
        }
        
        public virtual void Process(ProcessingScope context)
        {
            //Increment indices relative to table mapper                        
            var childScope = context.CreateChildScope(this);
         
            foreach (var item in SelectRowItems(context))
            {                
                BuildRows(childScope.Set(item));                
            }
        }

        protected virtual void BuildRows(ProcessingScope context)
        {
            foreach (var builderInfo in Builders)
            {
                //Add a row to each table builder
                if (builderInfo.Builder == null || builderInfo.Builder.AddRowFromContext(context))
                {
                    //Process nested table mappers
                    foreach (var child in builderInfo.Children)
                    {
                        child.Process(context);
                    }
                }
            }
        }

        protected abstract IEnumerable SelectRowItems(ProcessingScope context);


        protected class BuilderInfo
        {
            public TableDataBuilder Builder { get; set; }

            public List<ITableMapper> Children { get; set; }

            public BuilderInfo(TableDataBuilder builder)
            {
                Builder = builder;
                Children = new List<ITableMapper>();
            }
        }
    }
}