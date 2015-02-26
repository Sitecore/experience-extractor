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
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Components.PostProcessors;
using ExperienceExtractor.Processing;
using Sitecore.Analytics;
using Sitecore.Analytics.Model;

namespace ExperienceExtractor.Components.Parsing.PostProcessors
{
    [ParseFactory("mssql", "Export to SQL Server", "Exports the data to a Microsoft SQL Server, and optionally creates a database with a schema matching the extracted data"),
        ParseFactoryParameter("Connection", typeof(string), "A connection string or name of an connection string for the target SQL Server", required: true, isMainParameter: true),
        ParseFactoryParameter("CreateDatabase", typeof(string), "The name of the database to create. If omitted the target database is assumed to exist and have a schema matching the extracted data")]
    public class MsSqlFactory : IParseFactory<ITableDataPostProcessor>
    {
        public ITableDataPostProcessor Parse(JobParser parser, ParseState state)
        {
            var connectionString = state.Require<string>("Connection", true);
            if (!connectionString.Contains("="))
            {
                connectionString = ConfigurationManager.ConnectionStrings[connectionString].ConnectionString;
            }
                                    
            return new SqlExporter(connectionString, state.TryGet<string>("CreateDatabase"));
        }
    }
}
