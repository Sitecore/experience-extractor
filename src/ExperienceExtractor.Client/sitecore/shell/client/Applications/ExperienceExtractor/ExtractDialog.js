define(["sitecore", "experienceExtractor"], function (sc, X) {	
	return sc.Definitions.App.extend({
		initialized: function() {			
			var _this = this;			
			_this.on("extract:dialog:button:cancel:clicked", function () { _this.ExtractDialog.hide(); }, _this);			
		},
		
		extract: function() {			
			this.ExtractDialog.show();								
			this.ExperienceExtractorStatus.extract();
		}
	});
});