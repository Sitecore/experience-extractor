<%-- 
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
--%>

<%@ Page Language="C#" AutoEventWireup="true" Inherits="Sitecore.sitecore.admin.AdminPage" %>
<%@ Import Namespace="ExperienceExtractor.Api.Http.Configuration" %>
<script runat="server">
	protected void Page_Load(object sender, EventArgs e)
    {
	    if (!ExperienceExtractorWebApiConfig.AllowAnonymousAccess)
	    {
	        CheckSecurity();
	    }
    }
</script>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
    <title></title>
    <script src="//code.jquery.com/jquery-2.1.3.min.js"></script>
    
    <style>
        #cancel-job { display: none;}

        #job{ width: 100%;}

        .error { color: red; font-weight: bold;}
		
		.status-failed { color: #990000; }
		.status-completed { color: #009900; }

        #editor {
			position: absolute;
			top: 0;
			left: 0;
			right: 0;
			bottom: 202px;
        }
		
		#submit-panel {
			position: absolute;			
			border-top: 2px solid #999;			
			left: 0;
			right: 0;
			bottom: 0;
			height: 160px;
			padding: 20px;
			overflow-y: scroll;
			background-color: #eee;
			font-family: "Monaco", "Menlo", "Ubuntu Mono", "Droid Sans Mono", "Consolas", monospace;			
		}
		
		#submit-panel button {
			width: 100%;
			height: 40px;						
		}
		
		#submit-panel a {
			text-align: center;
			display: block;
			margin: 10px 0;
		}
		
		.ace_editor, .ace_editor *
		{
			font-family: "Monaco", "Menlo", "Ubuntu Mono", "Droid Sans Mono", "Consolas", monospace !important;
			font-size: 16px !important;
			font-weight: bold!important;
			line-height: 1.25em!important;
			letter-spacing: 0 !important;
		}
		
		.ace_meta.ace_tag 
		{
			font-weight: normal!important;
		}
    </style>
</head>
<body>
    
	<div id="editor">

{
   "labels": "en-US", //Extract English labels
   "source": {
      "xdb": {         
         "filters": [
            {"daterange": { start: "2014-01-01Z" }}, //All visits since Jan 1 2014
            {"limit": 1000} //Only extract the first thounsand visits
            //{"sample": 0.5} //Uncomment to extract a random sample with 50 % of the visits
         ]
      }
   },
   "mapper": {
      "tables": [
         {
            "name": "MonthlyVisits", //Main table with name "Visits"
            "fields": [
               {"date": {"resolution": "month"}}, //or "year", "quarter", "day"
               //{"time": "hour"} //Uncomment on add time of day dimension
               {"dimension": {
                 "name": "Geo", //Create a dimension table with country, region and city from Experience Analytics
                 "fields": [
                    {"xa": "Visits/By Country"},
                    {"xa": "Visits/By Region"},
                    {"xa": "Visits/By City"}
                 ]
               }},
               {"xa": "Visits/By Campaign"}, //Include campaign from Experience Analytics
               "facts" //Add the 6 Experience Analytics facts, Visits, Value, Bounces, Conversions, TimeOnSite, PageViews, Count
            ]
         }
      ]
   },
   
   postprocessors: [
      //Uncomment and update the connection string to export the data to a new database that will be created with the name "ExperienceExtractorTest"
      //{"mssql": {"connection": "Server=.\\SQLEXPRESS;User Id=sa;Password=sa", createdatabase: "ExperienceExtractorTest"}}
      
      //Or export to Microsoft Access
      //"msaccess"
   ]
}  
  
	</div>

	<div id="submit-panel">
		<div id="status"></div>

		<button id="post-job">Submit</button>
		<button id="cancel-job">Cancel</button>
		
		 <xmp id="console"></xmp> 
     </div>
	 
    <script src="//cdnjs.cloudflare.com/ajax/libs/ace/1.1.3/ace.js"></script>
    <script src="//cdnjs.cloudflare.com/ajax/libs/js-yaml/3.2.6/js-yaml.min.js"></script>
	
    <script>
        $(function () {

            var yamlMode = document.location.hash == "#yaml";
            if (yamlMode) {
                $("#editor").text(jsyaml.dump(eval("(" + $("#editor").text() + ")")));
            }

            var apiRoot = "/<%=ExperienceExtractorWebApiConfig.JobApiRoute%>";

            var editor = ace.edit("editor");
            editor.setTheme("ace/theme/crimson_editor");

            editor.getSession().setUseWorker(false);
            editor.getSession().setTabSize(3);
            editor.getSession().setUseSoftTabs(true);
            editor.getSession().setMode("ace/mode/" + (yamlMode ? "yaml" : "json"));

            editor.setValue(editor.getValue().replace(/\t/g, "  "));

            var currentId = null;

            function updateConsole(ajax) {
                return ajax.done(function (data, textStatus, xhr) {
                    if (data.ResultUrl) {
                        $("#status").empty().append($("<a>Click here to download result</a>").attr("href", data.ResultUrl));
                    } else {
                        $("#status").text(Math.round(100 * data.Progress * 100) / 100 + " % complete (" + data.Status + ")");
                    }

                    $("#console").html((data.LastException ? data.LastException + "\r\n\r\n" : "") + xhr.responseText);
                    $("#console").attr("class", "status-" + (data.Status || "unkown").toLowerCase());
                    if (data.Ended) {
                        updateState(false);
                        currentId = null;
                    } else {
                        updateState(true);
                        currentId = data.Id;
                    }

                }).fail(function (xhr) {
                    $("#console").addClass("error").html(xhr.responseText);
                    currentId = null;
                });
            }

            $("#post-job").click(function () {

                try {
                    var job = yamlMode ? jsyaml.safeLoad(editor.getValue()) : eval("(" + editor.getValue() + ")");
                    console.log(job);
                    updateConsole($.ajax({
                        url: apiRoot,
                        type: "POST",
                        data: JSON.stringify(job),
                        contentType: "application/json; charset=utf-8"
                    }));
                } catch (e) {
                    console.log(e);
                    $("#console").text("" + e);
                }
            });

            $("#cancel-job").click(function () {
                if (currentId) {
                    $.ajax({ url: apiRoot + "/" + currentId, type: "DELETE", contentType: "application/json; charset=utf-8" }).done(function () {
                        updateState(false);
                    });
                }
            });

            function updateState(running) {
                $("#post-job").css("display", running ? "none" : "block");
                $("#cancel-job").css("display", running ? "block" : "none");
            }

            var to;
            function poll() {
                clearTimeout(to);
                if (currentId) {
                    updateConsole($.ajax({
                        url: apiRoot + "/" + currentId,
                        type: "GET",
                        contentType: "application/json; charset=utf-8"
                    })).always(function () {
                        to = setTimeout(poll, 250);
                    });
                } else {
                    to = setTimeout(poll, 250);
                }
            }
            poll();

            editor.focus();
        });
    </script>        
	
</body>
</html>
