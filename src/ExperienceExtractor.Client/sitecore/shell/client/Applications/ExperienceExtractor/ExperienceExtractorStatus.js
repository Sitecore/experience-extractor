
define(["sitecore", "jquery", "experienceExtractor"], function (_sc, $, X) {

	_sc.Factories.createBaseComponent({
		name: "ExperienceExtractorStatus",
		base: "ControlBase",
		selector: ".sc-experience-extractor-status",
		attributes: [
			{ name: "apiUrl", value: "$el.data:sc-extract-api" }
		],
		initialize: function() {			
			
		},
		
		getProgressBar: function() {			
			return this.app[this.model.get("name") + "ProgressBar"].viewModel;
		},
		
		extendModel: {
			extract: function() {						
				var _this = this;						
				var model = this.viewModel;		
				var bar = _this.viewModel.getProgressBar();				
				var status = $(".status-text", model.$el);
				var progress = 0;
				function update() {
					progress += 0.1;							
					bar.percentage(progress*100);
					if( progress >= 1 ) {
						bar.hide();
					} else {
						bar.show();
						setTimeout(update, 100);
					}
				}
				
				update();
			}
		}
	});
});