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

define(["sitecore", "jquery"], function (Sitecore, $) {
	$("<link type='text/css' rel='stylesheet' />").attr("href", "/sitecore/shell/client/Applications/ExperienceExtractor/ExperienceExtractor.css")
		.appendTo("head");

	var xaDimensionConfig = {
		tableDimensions: ["Pages/By Page", "Pages/By Page URL"],
		extractDimensionTables: {
			"Geo": ["Visits/By Country", "Visits/By Region", "Visits/By City"]
		},
		
		sortOrder: {
			"Visits/By Country": 0,
			"Visits/By Region": 1,
			"Visits/By City": 2
		}
	}		
	
		
	return {
		job: {
			source: {"xdb":{filters:[]}},		
			mapper:{},		
			postprocessors: ["msaccess"]
		},
		xaDimensionConfig: xaDimensionConfig
	};
});