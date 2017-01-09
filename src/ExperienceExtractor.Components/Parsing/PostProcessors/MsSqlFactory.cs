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

using System.Configuration;
using System.Linq;
using System.Spatial;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Components.PostProcessors;
using ExperienceExtractor.Mapping;
using ExperienceExtractor.Processing;
using Sitecore.Analytics;
using Sitecore.Analytics.Model;

namespace ExperienceExtractor.Components.Parsing.PostProcessors
{
    [ParseFactory("mssql", "Export to SQL Server", "Exports the data to a Microsoft SQL Server, and optionally creates a database with a schema matching the extracted data"),
        ParseFactoryParameter("Connection", typeof(string), "A connection string or name of an connection string for the target SQL Server", required: true, isMainParameter: true),
        ParseFactoryParameter("Database", typeof(string), "The name of the database to create. If omitted the target database is assumed to exist and have a schema matching the extracted data")]
    public class MsSqlFactory : IParseFactory<ITableDataPostProcessor>
    {
        public ITableDataPostProcessor Parse(JobParser parser, ParseState state)
        {

            var connectionString = state.TryGet("Connection", 
                ConfigurationManager.ConnectionStrings["ExperienceExtractorDump"].TryGet(c=>c.ConnectionString) ??
                    ConfigurationManager.ConnectionStrings["ExperienceExtractor.Sql"].TryGet(c => c.ConnectionString));

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ParseException(state, "A SQL Server connection string must be specified. Use the parameter 'Connection' or add a connection string with the name 'ExperienceExtractor.Sql'.");
            }

            if (!connectionString.Contains("="))
            {
                connectionString = ConfigurationManager.ConnectionStrings[connectionString].ConnectionString;
            }

            var ssasConnection = state.TryGet("SsasConnection", ConfigurationManager.ConnectionStrings["ExperienceExtractor.SsasTabular"].TryGet(c => c.ConnectionString));

            var ssasDatabase = state.TryGet<string>("SsasDatabase");          

            var exporter = new SqlExporter(connectionString,
                state.TryGet("Database", () => state.TryGet<string>("CreateDatabase")), state.TryGet<bool>("clearInsteadOfDropCreate"));

            exporter.SqlClearOptions = state.TryGet("Clear", SqlClearOptions.None);
            exporter.Rebuild = state.TryGet("Rebuild", false);
            
            if( !string.IsNullOrEmpty(ssasDatabase))
            {                
                if (string.IsNullOrEmpty(ssasConnection))
                {
                    throw new ParseException(state,
                        "A connection string for SQL Server Analysis Services running in tabular mode must be specified. Use the parameter 'SsasConnection' or add a connection string with the name 'ExperienceExtractor.SsasTabular'."); 
                }
                exporter.SsasConnectionString = ssasConnection;
                exporter.SsasDbName = ssasDatabase;                
            };
            
            exporter.Update = state.TryGet("Update", false);
            exporter.SsasOnly = state.TryGet("SsasOnly", false);

            return exporter;
        }
    }
}
