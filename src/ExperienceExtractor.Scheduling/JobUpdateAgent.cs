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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ExperienceExtractor.Api.Jobs;
using ExperienceExtractor.Api.Parsing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore;
using Sitecore.Analytics.Core;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.SecurityModel;

namespace ExperienceExtractor.Scheduling
{
    public class JobUpdateAgent : IAgent
    {
        private static readonly ID ScheduledJobTemplateId = new ID("{F4F459A3-09DF-4BFA-9083-8749AD53235F}");

        private readonly string _rootItem;        

        public JobUpdateAgent(string rootItem)
        {
            _rootItem = rootItem;
        }

        public void Execute()
        {
            using (new SecurityDisabler())
            {
                var jobRoot = Database.GetDatabase("master").GetItem(_rootItem);

                foreach (
                    var jobItem in
                        jobRoot.Axes.GetDescendants()
                            .Where(j => j.TemplateID == ScheduledJobTemplateId))
                {
                    
                    try
                    {
                        var lastRun = DateUtil.ParseDateTime(jobItem["Last success"], DateTime.MinValue);

                        if (jobItem["Suspended"] == "1") continue;
                        if (!string.IsNullOrWhiteSpace(jobItem["Update interval"]))
                        {
                            var interval = TimeSpan.Parse(jobItem["Update interval"]);
                            if( lastRun + interval > DateTime.Now) continue;                            
                        }


                        var type = jobItem["Type"];
                        var factory = ParseFactories.Default.Get<IJobLoader>(type);
                        if (factory == null)
                        {
                            throw new Exception(string.Format("No job loader factory is registered for '{0}'", type));
                        }

                        JToken spec;
                        try
                        {
                            spec = JObject.Parse(jobItem["Specification"]);
                        }
                        catch
                        {
                            spec = new JValue(jobItem["Specification"]);
                        }                    
                                                                    
                        var jobLoader = factory.Parse(null, new JsonParseState(spec, null));
                        var jobSpec = new UpdateJobWrapper(jobLoader.Load(), lockKey: jobItem.ID.ToString());


                        var fixedJobItem = jobItem;
                        var jobInfo = ExperienceExtractorApiContainer.JobRepository.CreateJob(jobSpec, job =>
                        {
                            using(new SecurityDisabler())
                            using (new EditContext(fixedJobItem))
                            {
                                if (job.Status == JobStatus.Completed)
                                {
                                    fixedJobItem["Last success"] = DateUtil.IsoNow;
                                }
                                fixedJobItem["Last status"] = JsonConvert.SerializeObject(job, Formatting.Indented);
                            }
                        });    
                    
                        Log.Info(
                            string.Format("Experience Extractor - Updating '{0}' (Job ID: {1})", jobItem.Name, jobInfo.Id),
                            this);

                        using (new EditContext(jobItem))
                        {
                            jobItem["Last run"] = DateUtil.IsoNow;
                        }
                        //context.Response.Write(string.Format(@"<a href='http://sc80rev150427/sitecore/experienceextractor/jobs/{0}' target='_blank'>{0}</a><br />", job.Id));
                    }
                    catch (Exception ex)
                    {
                        Log.SingleError(
                            string.Format("Experience Extractor - Invalid spec for scheduled job {0}. ({1})", jobItem.Name,
                                ex.Message), this);
                    }
                }
            }
        }
    }
}
