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
using System.Globalization;
using System.Linq;
using System.Net;
using Sitecore.Data;
using Sitecore.Diagnostics;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Export;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Mapping.Splitting;
using ExperienceExtractor.Processing;
using ExperienceExtractor.Processing.DataSources;
using Sitecore.Globalization;

namespace ExperienceExtractor.Api.Parsing
{
    public abstract class JobParser : IJobSpecification
    {
        protected ParseFactories Configuration { get; private set; }

        protected abstract ParseState RootState { get; }

        public virtual Language DefaultLanguage
        {
            get
            {
                var language = RootState.TryGet<string>("Language");

                return string.IsNullOrEmpty(language) ? Language.Current
                    : Language.Parse(language);
            }
        }        
                
        protected JobParser(ParseFactories configuration = null)
        {            
            Configuration = configuration ?? ExperienceExtractorApiContainer.ParseFactories;
        }

        protected abstract TType Parse<TType>(ParseState state);

        public Database Database { get; set; }

        public virtual IDataSource ParseDataSource(ParseState state)
        {
            return Parse<IDataSource>(state);
        }

        public virtual IDataFilter ParseDataFilter(ParseState state)
        {
            return Parse<IDataFilter>(state);
        }

        public virtual IFieldMapper ParseFieldMapper(ParseState state)
        {
            return Parse<IFieldMapper>(state);
        }

        public virtual ITableMapper ParseTableMapper(ParseState state)
        {
            return Parse<ITableMapper>(state);
        }

        public virtual TableDefinition ParseTableDefinition(ParseState state)
        {
            try
            {
                return Parse<TableDefinition>(state);
            }
            catch (KeyNotFoundException)
            {
                var merge = state.SelectMany("Merge").Select(ParseTableDefinition).ToArray();

                var fieldMappers = state.ClearAffix().SelectMany("Fields").Select(ParseFieldMapper)
                    .Concat(merge.SelectMany(bundle => bundle.FieldMappers)).ToArray();

                var tableMappers = state.SelectMany("Tables").Select(ParseTableMapper)
                    .Concat(merge.SelectMany(bundle => bundle.TableMappers)).ToArray();

                var name = state.TryGet<string>("Name");
                if (string.IsNullOrEmpty(name) && fieldMappers.Length > 0)
                {
                    throw ParseException.AttributeError(state, "Name is required for table definitions with fields");
                }
                if (tableMappers.Length == 0 && fieldMappers.Length == 0)
                {
                    throw ParseException.AttributeError(state,
                        "Expected at least one field or table in table definition");
                }

                return new TableDefinition(state.AffixName(name))
                {
                    FieldMappers = fieldMappers,
                    TableMappers = tableMappers
                };
            }
        }

        public virtual ITableDataPostProcessor ParseTableDataPostProcessor(ParseState state)
        {
            return Parse<ITableDataPostProcessor>(state);
        }

        public virtual ISplitter ParseSplitter(ParseState state)
        {
            return Parse<ISplitter>(state);
        }

        public virtual IDataSource CreateDataSource()
        {
            return ParseDataSource(RootState.Select("Source", true));
        }

        public virtual ITableMapper CreateRootMapper()
        {
            var mapper = RootState.Select("Mapper");
            return mapper != null ? ParseTableMapper(mapper) : null;
        }


        public virtual IEnumerable<ITableDataPostProcessor> CreatePostProcessors()
        {
            return RootState.SelectMany("PostProcessors").Select(ParseTableDataPostProcessor);
        }


        public virtual ITableDataExporter CreateExporter(string tempPath)
        {
            var cultureName = "en-US";
            var delim = "\t";

            var csvConfig = RootState.Select("export");
            if (csvConfig != null)
            {
                cultureName = csvConfig.TryGet("culture", cultureName);
                delim = csvConfig.TryGet("delim", delim);
            }
            

            return new CsvExporter(tempPath, delim, CultureInfo.GetCultureInfo(cultureName));
        }

        public virtual void Initialize(Job job)
        {
            try
            {
                Database = Database.GetDatabase(job.ExecutionSettings.DatabaseName);
            }
            catch(Exception ex) { Log.Error("Error initializing database", ex, this); }

            job.JobEnded += (sender, args) =>
            {
                var export = RootState.Select("export");
                if (export != null)
                {
                    var ping = export.TryGet<string>("ping");
                    if (!string.IsNullOrEmpty(ping))
                    {
                        new WebClient().DownloadString(
                            ping.Replace("{id}", job.Id.ToString("D"))
                                .Replace("{status}", Enum.GetName(typeof (JobStatus), job.Status)));
                    }
                }
            };
        }

    }
}
