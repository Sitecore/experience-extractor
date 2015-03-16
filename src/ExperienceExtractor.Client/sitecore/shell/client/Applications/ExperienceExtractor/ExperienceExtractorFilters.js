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

require.config({
  paths: {
      dynatree: "/sitecore/shell/client/Speak/Assets/lib/ui/1.1/deps/DynaTree/jquery.dynatree-1.2.4",
      dynatreecss: "/sitecore/shell/client/Speak/Assets/lib/ui/1.1/deps/DynaTree/skin-vista/ui.dynatree",
	  experienceExtractor: "/sitecore/shell/client/Applications/ExperienceExtractor/ExperienceExtractor"
  },    
  shim: {
      "dynatree": { deps: [ "jqueryui" ] }
  }
});


define(["sitecore", "jquery", "experienceExtractor", "dynatree"], function (_sc, $, X) {

	_sc.Factories.createBaseComponent({
		name: "ExperienceExtractorFitlers",
		base: "ControlBase",
		selector: ".sc-experience-extractor-filters",
		attributes: [],		
		initialize: function() {
			
						
			var _this = this;

			function formatDate(date) {
				var y = date.getFullYear();
				var m = date.getMonth() + 1;
				var d = date.getDate();
				
				if( m < 10 ) m = "0" + m;
				if( d < 10 ) d= "0" + d;
				
				return y + "-" + m + "-" + d + "Z";
			}
	
			var daterange, sampling;
			function updateJob() {								
				var from = _this.app[_this.model.get('name') + "FromDate"].viewModel.getDate();
				var to = _this.app[_this.model.get('name') + "ToDate"].viewModel.getDate();
				
				
				if( from || to ) {
					if( !daterange ) {						
						X.job.source.xdb.filters.push({"daterange": daterange = {}});
					}
					if( from ) daterange.start = formatDate(from);
					if( to ) daterange.end =  formatDate(to);
					
				}
				
				var samplingValue = _this.app[_this.model.get('name') + "Sampling"].get("selectedValue");
				if( !sampling ) {
					X.job.source.xdb.filters.push(sampling = {"sample": 1});
				}			
				sampling.sample = samplingValue / 100;
			}
			
			
			updateJob();
			$(".execute-button").click(function(e) {				
				updateJob();
				$("input[name='job']", _this.$el).val(JSON.stringify(X.job, null, 3));
			});
						
		}
	});
	
});