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

namespace ExperienceExtractor.Processing.Helpers
{
    using Api.Http.Configuration;

    public static class ProtocolHelper
    {
        /// <summary>
        /// If "forceProtocol" setting is used, this will return the corrected url.
        /// </summary>
        public static string EnforceProtocol(string url)
        {
            return string.IsNullOrWhiteSpace(ExperienceExtractorWebApiConfig.ForceProtocol)
                       ? url
                       : url.Replace("http://", string.Format("{0}://", ExperienceExtractorWebApiConfig.ForceProtocol));
        }
    }
}
