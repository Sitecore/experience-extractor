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
using System.Configuration;
using System.Data.SqlClient;
using System.Spatial;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Api.Parsing;
using ExperienceExtractor.Scheduling;
using Newtonsoft.Json.Linq;

namespace ExperienceExtractor.Components.Parsing.PostProcessors
{
    [ParseFactory("mssql", "SQL Server job loader")]
    public class MsSqlJobLoaderFactory : IParseFactory<IJobLoader>
    {
        public IJobLoader Parse(JobParser parser, ParseState state)
        {
            var connection = state.Require<string>("Database", true);

            if (!connection.Contains("="))
            {
                var builder =
                    new SqlConnectionStringBuilder(
                        ConfigurationManager.ConnectionStrings["ExperienceExtractor.Sql"].ConnectionString);
                builder.InitialCatalog = connection;
                connection = builder.ConnectionString;
            }

            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                if (DBNull.Value.Equals(new SqlCommand("SELECT OBJECT_ID('Sitecore.JobInfo', 'U')", conn).ExecuteScalar()))
                {
                    throw new ParseException(state, "Database does not contain the job info table. Was it created with Experience Extractor?");
                }

                var json = (string) new SqlCommand("SELECT TOP 1 [Prototype] FROM Sitecore.JobInfo", conn).ExecuteScalar();
                if (string.IsNullOrEmpty(json))
                {
                    throw new ParseException(state, "Database does not contain a job prototype");
                }

                var job = new JsonJobParser(JObject.Parse(json));
                return new StaticJobLoader(job);
            }
        }
    }
}
