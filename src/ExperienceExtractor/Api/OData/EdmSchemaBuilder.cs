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
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Spatial;
using System.Xml;
using System.Xml.Linq;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Data;
using ExperienceExtractor.Data.Schema;
using ExperienceExtractor.Export;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Csdl;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.Edm.Validation;

namespace ExperienceExtractor.Api.OData
{
    public class EdmSchemaBuilder : ITableDataPostProcessor
    {
        static EdmPrimitiveTypeKind GetEdmType(Type clrType)
        {
            clrType = Nullable.GetUnderlyingType(clrType) ?? clrType;
            if (clrType.IsEnum)
            {
                clrType = typeof (string);
            }
            return _typeMappings[clrType];
        }

        static bool IsNullable(Type clrType)
        {
            return clrType == typeof(string) || Nullable.GetUnderlyingType(clrType) != null;
        }

        public IEdmModel BuildModel(IEnumerable<TableData> tables)
        {
            var model = new EdmModel();            
            var container = new EdmEntityContainer("sitecore.com", "Visits");
            model.AddElement(container);            
            

            var tableMap = new Dictionary<TableDataSchema, EdmEntityTypeWrapper>();
            foreach (var table in tables)
            {
                var typeWrapper = AddTable(model, table);
                container.AddEntitySet(table.Name, typeWrapper.Type);
                tableMap.Add(table.Schema, typeWrapper);
            }

            foreach (var t in tableMap)
            {
                foreach (var reference in t.Key.RelatedTables)
                {
                    if (reference.RelationType == RelationType.Child || reference.RelationType == RelationType.DimensionReference)
                    {
                        var source = t.Value;
                        var target = tableMap[reference.RelatedTable];
                                                

                        t.Value.Type.AddBidirectionalNavigation(new EdmNavigationPropertyInfo()
                        {
                            Name = reference.RelatedTable.Name,
                            TargetMultiplicity = EdmMultiplicity.Many,                            
                            Target = target.Type
                        }, new EdmNavigationPropertyInfo()
                        {
                            Name = t.Key.Name,
                            TargetMultiplicity = reference.RelationType == RelationType.Child ? EdmMultiplicity.One : EdmMultiplicity.ZeroOrOne,
                            DependentProperties = reference.RelatedFields.Select(f=>target.Properties[f]),
                            Target = source.Type
                        });
                    }
                }
            }

            return model;
        }

        EdmEntityTypeWrapper AddTable(EdmModel model, TableData dataBuilder)
        {
            var propertyMap = new Dictionary<Field, EdmStructuralProperty>();

            var table = new EdmEntityType("sitecore.com", dataBuilder.Name);
            foreach (var field in dataBuilder.Schema.Fields)
            {
                var prop = table.AddStructuralProperty(field.Name, GetEdmType(field.ValueType), IsNullable(field.ValueType));                
                if (field.FieldType == FieldType.Key)
                {
                    table.AddKeys(prop);
                }
                propertyMap.Add(field, prop);
            }            
            model.AddElement(table);            

            return new EdmEntityTypeWrapper {Type = table, Properties = propertyMap};
        }

        class EdmEntityTypeWrapper
        {
            public EdmEntityType Type { get; set; }
            public Dictionary<Field, EdmStructuralProperty> Properties { get; set; }
        }


        private static readonly Dictionary<Type, EdmPrimitiveTypeKind> _typeMappings = new Dictionary
            <Type, EdmPrimitiveTypeKind>()
        {
            {typeof (string), EdmPrimitiveTypeKind.String},
            {typeof (bool), EdmPrimitiveTypeKind.Boolean},
            {typeof (bool?), EdmPrimitiveTypeKind.Boolean},
            {typeof (byte), EdmPrimitiveTypeKind.Byte},
            {typeof (byte?), EdmPrimitiveTypeKind.Byte},
            {typeof (DateTime), EdmPrimitiveTypeKind.DateTime},
            {typeof (DateTime?), EdmPrimitiveTypeKind.DateTime},
            {typeof (decimal), EdmPrimitiveTypeKind.Decimal},
            {typeof (decimal?), EdmPrimitiveTypeKind.Decimal},
            {typeof (double), EdmPrimitiveTypeKind.Double},
            {typeof (double?), EdmPrimitiveTypeKind.Double},
            {typeof (Guid), EdmPrimitiveTypeKind.Guid},
            {typeof (Guid?), EdmPrimitiveTypeKind.Guid},
            {typeof (short), EdmPrimitiveTypeKind.Int16},
            {typeof (short?), EdmPrimitiveTypeKind.Int16},
            {typeof (int), EdmPrimitiveTypeKind.Int32},
            {typeof (int?), EdmPrimitiveTypeKind.Int32},
            {typeof (long), EdmPrimitiveTypeKind.Int64},
            {typeof (long?), EdmPrimitiveTypeKind.Int64},
            {typeof (sbyte), EdmPrimitiveTypeKind.SByte},
            {typeof (sbyte?), EdmPrimitiveTypeKind.SByte},
            {typeof (float), EdmPrimitiveTypeKind.Single},
            {typeof (float?), EdmPrimitiveTypeKind.Single},
            {typeof (byte[]), EdmPrimitiveTypeKind.Binary},
            {typeof (Stream), EdmPrimitiveTypeKind.Stream},
            {typeof (Geography), EdmPrimitiveTypeKind.Geography},
            {typeof (GeographyPoint), EdmPrimitiveTypeKind.GeographyPoint},
            {typeof (GeographyLineString), EdmPrimitiveTypeKind.GeographyLineString},
            {typeof (GeographyPolygon), EdmPrimitiveTypeKind.GeographyPolygon},
            {typeof (GeographyCollection), EdmPrimitiveTypeKind.GeographyCollection},
            {typeof (GeographyMultiLineString), EdmPrimitiveTypeKind.GeographyMultiLineString},
            {typeof (GeographyMultiPoint), EdmPrimitiveTypeKind.GeographyMultiPoint},
            {typeof (GeographyMultiPolygon), EdmPrimitiveTypeKind.GeographyMultiPolygon},
            {typeof (Geometry), EdmPrimitiveTypeKind.Geometry},
            {typeof (GeometryPoint), EdmPrimitiveTypeKind.GeometryPoint},
            {typeof (GeometryLineString), EdmPrimitiveTypeKind.GeometryLineString},
            {typeof (GeometryPolygon), EdmPrimitiveTypeKind.GeometryPolygon},
            {typeof (GeometryCollection), EdmPrimitiveTypeKind.GeometryCollection},
            {typeof (GeometryMultiLineString), EdmPrimitiveTypeKind.GeometryMultiLineString},
            {typeof (GeometryMultiPoint), EdmPrimitiveTypeKind.GeometryMultiPoint},
            {typeof (GeometryMultiPolygon), EdmPrimitiveTypeKind.GeometryMultiPolygon},
            {typeof (TimeSpan), EdmPrimitiveTypeKind.Time},
            {typeof (TimeSpan?), EdmPrimitiveTypeKind.Time},
            {typeof (DateTimeOffset), EdmPrimitiveTypeKind.DateTimeOffset},
            {typeof (DateTimeOffset?), EdmPrimitiveTypeKind.DateTimeOffset},

            // Keep the Binary and XElement in the end, since there are not the default mappings for Edm.Binary and Edm.String.
            {typeof (XElement), EdmPrimitiveTypeKind.String},
            {typeof (Binary), EdmPrimitiveTypeKind.Binary},
            {typeof (ushort), EdmPrimitiveTypeKind.Int32},
            {typeof (ushort?), EdmPrimitiveTypeKind.Int32},
            {typeof (uint), EdmPrimitiveTypeKind.Int64},
            {typeof (uint?), EdmPrimitiveTypeKind.Int64},
            {typeof (ulong), EdmPrimitiveTypeKind.Int64},
            {typeof (ulong?), EdmPrimitiveTypeKind.Int64},
            {typeof (char[]), EdmPrimitiveTypeKind.String},
            {typeof (char), EdmPrimitiveTypeKind.String},
            {typeof (char?), EdmPrimitiveTypeKind.String},
        };

        public string Name { get { return "EdmSchema"; }}

        public void Process(string tempDirectory, IEnumerable<TableData> tables, IJobSpecification job)
        {
            var model = BuildModel(tables);
            using (var xml = XmlWriter.Create(Path.Combine(tempDirectory, "edm.xml")))
            {
                IEnumerable<EdmError> errors;
                model.TryWriteCsdl(xml, out errors);
            }
        }

        public void Validate(IEnumerable<TableData> tables, IJobSpecification job)
        {
            
        }

        public bool UpdateDataSource(IEnumerable<TableData> tables, IDataSource source)
        {
            return false;
        }        
    }
}