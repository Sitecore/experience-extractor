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
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Data;
using ExperienceExtractor.Export;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;
using Newtonsoft.Json.Linq;
using Sitecore.Globalization;

namespace ExperienceExtractor.Scheduling
{

    public class UpdateJobWrapper : IJobSpecification
    {
        private readonly string _lockKey;
        public IJobSpecification Prototype { get; set; }
        public bool Rebuild { get; set; }
        
        public UpdateJobWrapper(IJobSpecification prototype, bool rebuild = false, string lockKey = null)
        {
            _lockKey = lockKey;
            Prototype = prototype;
            Rebuild = rebuild;            
        }

        public virtual string LockKey
        {
            get { return _lockKey ?? Prototype.LockKey; }
        }

        public Language DefaultLanguage
        {
            get { return Prototype.DefaultLanguage; }
        }

        public IDataSource CreateDataSource()
        {
            return Prototype.CreateDataSource();
        }

        public ITableMapper CreateRootMapper()
        {
            return Prototype.CreateRootMapper();
        }

        public IEnumerable<ITableDataPostProcessor> CreatePostProcessors()
        {
            foreach (var proc in Prototype.CreatePostProcessors())
            {
                var updateProc = proc as IUpdatingTableDataPostProcessor;
                if (updateProc != null)
                {
                    if (Rebuild)
                    {
                        updateProc.AdjustToRebuild();
                    }
                    else
                    {
                        updateProc.AdjustToUpdate();
                    }
                }

                yield return proc;
            }
        }

        public ITableDataExporter CreateExporter(string tempPath)
        {
            return Prototype.CreateExporter(tempPath);
        }

        public IList<TableData> Load(string tempPath)
        {
            return Prototype.Load(tempPath);
        }

        public void Initialize(Job job)
        {
            Prototype.Initialize(job);
        }

        public override string ToString()
        {
            return Prototype.ToString();
        }
    }
}
