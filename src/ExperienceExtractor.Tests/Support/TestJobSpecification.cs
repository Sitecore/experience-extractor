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
using Sitecore.Analytics.Aggregation.Data.Model;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Data;
using ExperienceExtractor.Export;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;
using Sitecore.Globalization;

namespace ExperienceExtractor.Tests.Support
{
    class TestJobSpecification : IJobSpecification
    {
        private readonly IEnumerable<IVisitAggregationContext> _contexts;
        private readonly Func<ITableMapper> _mapper;
        private TestExporter _exporter = new TestExporter();

        

        public TestJobSpecification(IEnumerable<IVisitAggregationContext> contexts, Func<ITableMapper> mapper)
        {
            _contexts = contexts;
            _mapper = mapper;
        }

        public IEnumerable<TableData> Tables
        {
            get { return _exporter.Tables; }
        }

        public Language DefaultLanguage { get { return Language.Parse("en"); }}

        public IDataSource CreateDataSource()
        {
            return new SimpleDataSource(_contexts);
        }

        public ITableMapper CreateRootMapper()
        {
            return _mapper();
        }

        public IEnumerable<ITableDataPostProcessor> CreatePostProcessors()
        {
            yield break;
        }
        
        public ITableDataExporter CreateExporter(string tempPath)
        {
            return _exporter;
        }

        public void Initialize(Job job)
        {
            
        }
    }
}
