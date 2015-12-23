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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Routing;
using System.Xml;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using ExperienceExtractor.Api.Http.Configuration;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Processing.Helpers;
using Sitecore.ExperienceAnalytics.Api;
using Sitecore.IO;
using Sitecore.Pipelines;

namespace ExperienceExtractor.Api.Pipelines
{
    class ExperienceExtractorApiInitializer
    {
        public void Process(PipelineArgs args)
        {
            Log.Info("Initializing ExperienceExtractor", this);                        

            var exportNode = Factory.GetConfigNode("experienceExtractor") as XmlElement;
            if (exportNode != null)
            {
                exportNode.GetAttribute("apiRoute").SetIfDefined(value=>
                    ExperienceExtractorWebApiConfig.JobApiRoute = value);
                
                var security = exportNode.SelectSingleNode("security") as XmlElement;
                if (security != null)
                {
                    ExperienceExtractorWebApiConfig.AllowAnonymousAccess =
                        XmlConvert.ToBoolean(security.GetAttribute("allowAnonymousAccess"));

                    ExperienceExtractorWebApiConfig.AllowedRoles = exportNode.SelectNodes("allowedRole").OrEmpty()
                        .OfType<XmlElement>().Select(el => el.InnerText.Trim()).ToList();

                    ExperienceExtractorWebApiConfig.AllowedUsers = exportNode.SelectNodes("allowedUser").OrEmpty()
                        .OfType<XmlElement>().Select(el => el.InnerText.Trim()).ToList();
                }

                exportNode.GetAttribute("tempDirectory").SetIfDefined(value =>
                    ExperienceExtractorWebApiConfig.JobExecutionSettings.TempDirectory = FileUtil.MapPath(value));

                exportNode.GetAttribute("batchSize").SetIfDefined(value =>
                    ExperienceExtractorWebApiConfig.JobExecutionSettings.BatchSize = XmlConvert.ToInt32(value));
                
                exportNode.GetAttribute("sizeLimit").SetIfDefined(value=>                
                    ExperienceExtractorWebApiConfig.JobExecutionSettings.SizeLimit = XmlConvert.ToInt64(value));

                exportNode.GetAttribute("processingThreads").SetIfDefined(value =>
                    ExperienceExtractorWebApiConfig.JobExecutionSettings.ProcessingThreads = XmlConvert.ToInt32(value));

                exportNode.GetAttribute("loadThreads").SetIfDefined(value =>
                    ExperienceExtractorWebApiConfig.JobExecutionSettings.LoadThreads = XmlConvert.ToInt32(value));
                
                exportNode.GetAttribute("dataSourceBufferSize").SetIfDefined(value =>
                    ExperienceExtractorWebApiConfig.JobExecutionSettings.DataSourceBufferSize = XmlConvert.ToInt32(value));

                exportNode.GetAttribute("database").SetIfDefined(value=>
                    ExperienceExtractorWebApiConfig.JobExecutionSettings.DatabaseName = value);

                exportNode.GetAttribute("xdbConnection").SetIfDefined(value =>
                    ExperienceExtractorWebApiConfig.XdbConnectionString = value);

                exportNode.GetAttribute("maxJobHistoryLength").SetIfDefined(value=>
                    ExperienceExtractorWebApiConfig.MaxJobHistoryLength = XmlConvert.ToInt32(value));

                exportNode.GetAttribute("forceProtocol").SetIfDefined(value =>
                    ExperienceExtractorWebApiConfig.ForceProtocol = value);


                var repositoryNode = exportNode.SelectSingleNode("jobRepository");
                if (repositoryNode != null)
                {
                    ExperienceExtractorApiContainer.JobRepository = Factory.CreateObject<IJobRepository>(repositoryNode);
                }

                var nodes = exportNode.SelectNodes("parsing/assembly").OrEmpty().OfType<XmlElement>().ToArray();
                foreach (var assemblyName in nodes)
                {
                    try
                    {
                        ExperienceExtractorApiContainer.ParseFactories.InitializeFromAttributes(
                            Assembly.Load(assemblyName.InnerText.Trim()));
                        Log.Info("Initialized parse factories from " + assemblyName.InnerText.Trim(), this);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading parse factories from (" + assemblyName.InnerText + ")", ex, this);
                        var rte = ex as ReflectionTypeLoadException;
                        if (rte != null)
                        {
                            foreach (var le in rte.LoaderExceptions)
                            {
                                Log.Error("Error loading parse factories", le, this);
                            }
                        }
                    }
                }

                foreach (var path in exportNode.SelectNodes("itemRoots/*").OrEmpty().OfType<XmlElement>())
                {
                    ExperienceExtractorApiContainer.ItemPaths[path.Name] = path.InnerText.Trim();
                }
            }            
            GlobalConfiguration.Configure(ExperienceExtractorWebApiConfig.Register);            
            
        }
    }
}
