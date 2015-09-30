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
using System.Web.Http;
using System.Web.Http.Controllers;
using ExperienceExtractor.Api.Http.Configuration;
using Sitecore;

namespace ExperienceExtractor.Api.Http
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireSitecoreLoginAttribute : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            return CheckSecurity();
        }

        public static bool CheckSecurity()
        {
            if (ExperienceExtractorWebApiConfig.AllowAnonymousAccess) return true;

            var user = Context.User;
            return user != null && user.IsAuthenticated &&
                   (user.IsAdministrator || ExperienceExtractorWebApiConfig.AllowedRoles.Any(user.IsInRole) || ExperienceExtractorWebApiConfig.AllowedUsers.Any(userName => userName.Equals(user.Name, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}
