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
using System.Net.Http.Headers;
using System.Web.Hosting;
using System.Web.Http;
using ExperienceExtractor.Api.Jobs;
using Sitecore.Diagnostics;
using RouteParameter = System.Web.Http.RouteParameter;

namespace ExperienceExtractor.Api.Http.Configuration
{
    public static class ExperienceExtractorWebApiConfig
    {
        public static string RoutePrefix { get; private set; }

        public static string JobRouteName { get { return RoutePrefix + ".Job"; } }
        public static string JobResultRouteName { get { return RoutePrefix + ".JobResults"; } }
        public static string ParseFactoryRouteName { get { return RoutePrefix + ".ParseFactories"; } }        

        public static bool AllowAnonymousAccess { get; set; }

        public static List<string> AllowedRoles { get; set; }
        public static List<string> AllowedUsers { get; set; }

        public static string JobApiRoute { get; set; }

        public static int FieldCacheSize { get; set; }

        public static string XdbConnectionString { get; set; }

        public static int MaxJobHistoryLength { get; set; }

        public static string ForceProtocol { get; set; }

        /// <summary>
        /// Maximum expected lag before interactions are saved in xDB.
        /// </summary>
        public static TimeSpan XdbLag { get; set; }

        public static JobExecutionSettings JobExecutionSettings { get; set; }

        static ExperienceExtractorWebApiConfig()
        {
            JobApiRoute = "api/jobs";

            AllowedRoles = new List<string>();
            AllowedUsers = new List<string>();

            RoutePrefix = typeof(ExperienceExtractorWebApiConfig).Assembly.FullName;

            XdbConnectionString = "ExperienceExtractor";
            
            JobExecutionSettings = new JobExecutionSettings
            {
                TempDirectory = HostingEnvironment.MapPath("~/App_Data/Export"),
                FieldCacheSize = 5000,
                DatabaseName = "master",
                BatchSize = 50000,
                LoadThreads = 2,
                ProcessingThreads = 2,
                DataSourceBufferSize = 500,
                SizeLimit = 2 * 1024 * 1024 * 1024L
            };

            MaxJobHistoryLength = 100;
        }


        public static void Register(HttpConfiguration config)
        {
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            config.Formatters.JsonFormatter.Indent = true;

            config.Routes.MapHttpRoute(
                name: ParseFactoryRouteName,
                routeTemplate: JobApiRoute + "/metadata/{action}",
                defaults: new { controller = "ExperienceExtractorMetaData", action = "ParseFactories" }
            );

            //Jobs

            config.Routes.MapHttpRoute(
                name: JobResultRouteName,
                routeTemplate: JobApiRoute + "/{id}/result",
                defaults: new { controller = "ExperienceExtractorJobResults" }
            );

            config.Routes.MapHttpRoute(
                name: JobRouteName,
                routeTemplate: JobApiRoute + "/{id}",
                defaults: new { controller = "ExperienceExtractorJobs", id = RouteParameter.Optional }
            );

            //GlobalConfiguration.Configuration.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always;
            Log.Info(string.Format("Initailized Experience Extractor API at {0}", JobApiRoute), typeof(ExperienceExtractorWebApiConfig));
        }
    }
}
