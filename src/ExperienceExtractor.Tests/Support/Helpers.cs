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
using Sitecore.Analytics.Aggregation.Data.Model;
using ExperienceExtractor.Data;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.Helpers;

namespace ExperienceExtractor.Tests.Support
{
    static class Helpers
    {
        public static Guid ToGuid(this int n)
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(n).CopyTo(bytes, 0);
            return new Guid(bytes);
        }

        public static TValue Field<TValue>(this TableData table, string name, object[] row)
        {
            var index = table.Schema.Fields.AsIndexed().FirstOrDefault(f => f.Value.Name == name).Index;
            return (TValue) row[index];
        }

        public static IEnumerable<TValue> Fields<TValue>(this TableData table, string name, IEnumerable<object[]> rows = null)
        {
            var index = table.Schema.Fields.AsIndexed().FirstOrDefault(f => f.Value.Name == name).Index;
            return (rows ?? table.Rows).Select(row => (TValue) row[index]);
        }

        public static IEnumerable<TableData> Process(this IEnumerable<IVisitAggregationContext> contexts,
            Func<TableDefinition> defintion)
        {
            return contexts.Process(()=>new SimpleTableMapper(defintion()));
        }

        public static IEnumerable<TableData> Process(this IEnumerable<IVisitAggregationContext> contexts, Func<ITableMapper> mapper, Action<DataProcessor> initializer = null)
        {
            var spec = new TestJobSpecification(contexts, mapper);

            var processor = new DataProcessor(spec.CreateRootMapper());
            if (initializer != null)
            {
                initializer(processor);
            }
            processor.Process(spec.CreateDataSource());

            return processor.Tables;
        }
    }
}
