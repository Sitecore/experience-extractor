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
		name: "ExperienceExtractor",
		base: "ControlBase",
		selector: ".sc-experience-extractor-dimensions",
		attributes: [],		
		initialize: function() {
					
			function isTableDimension(data) {
				return X.xaDimensionConfig.tableDimensions.filter(function(t) { return t == data.xaPath; }).length;
			}
			
			function getDimensionName(data) {
				var extractDimensions = X.xaDimensionConfig.extractDimensionTables;
				for( var k in extractDimensions ) {
					for( var i = 0; i < extractDimensions[k].length; i++ ) {
						if( data.xaPath == extractDimensions[k][i] ) return k;
					}
				}
				
				return null;
			}
			
			function getDimensionField(table, name) {
				for( var i = 0; i < table.fields.length; i++ ) {
					for( var k in table.fields[i] ) {
						if( k == "dimension" ) {
							if( table.fields[i][k].name == name ) {
								return table.fields[i][k];
							}
						}
					}
				}
				
				var dimension = {name: name, fields: []};
				table.fields.push({"dimension": dimension});
				return dimension;
			}
					
			function updateJob() {			
				var visitsTable = {name: "Visits", fields: [], tables: []};
			
				X.job.mapper = { tables: [visitsTable]};
											
				var nodes = $(".xa-tree").dynatree("getSelectedNodes").slice(0);					
				nodes.sort(function(x,y) {
					x = x.data.sortOrder;
					y = y.data.sortOrder;
					return x == y ? 0 : x < y ? -1 : 1;
				});
				nodes.forEach(function(node) {
					if( node.data.updateJob ) {
						node.data.updateJob(visitsTable, node);
					}
					else if( node.data.xaDimension ) {
						if( isTableDimension(node.data) ) {
							visitsTable.tables.push({"xa": node.data.xaPath});
						} else {		
							var extractDimension = getDimensionName(node.data);
							var target = extractDimension ? getDimensionField(visitsTable, extractDimension) : visitsTable;							
							target.fields.push({"xa": node.data.xaPath});
						}
					}					
				});	
				visitsTable.fields.push("facts");				
			}
			
			function foldItems(items) {
			
				items = items.filter(function(item) { return item.$templateName == "DimensionFolder" || item.$templateName == "Dimension"});
			
				var itemsByPath = {};
				items.forEach(function(item) {itemsByPath[item.$path] = item;});			
				
				var itemsByParent = {};				
				var root = [];
				items.forEach(function(item) {
					var path = item.$path.split("/");					
					var parent = path.slice(0, path.length - 1).join("/");
					if( !itemsByPath[parent] ) {
						root.push(item);
					} else {
						(itemsByParent[parent] = itemsByParent[parent] || []).push(item);
					}
				});
				
				(function fold(itemsArray) {
					itemsArray.forEach(function(item) {
						item.children = itemsByParent[item.$path] || [];						
						fold(item.children);
					});
				})(root);
											
				return root;
			}
			
			function createFolder(title, key, initializer) {
				initializer = initializer || function(node){return node;};
				return initializer({title: title, isFolder: true, hideCheckbox: true, key: key, icon: "/temp/IconCache/Applications/16x16/folder_document.png", children: []});
			}
			function createDimension(title, key, initializer) {
				initializer = initializer || function(node){return node;};
				return initializer({title: title, isFolder: false, key: key, icon: "/temp/IconCache/Business/16x16/tables.png", children: []});
			}
			
			function setDropDownEditor(data, options) {
				data.setEditor = function(container, node) {
					var editor = $("<select></select>").change(function() {
						node.data.selectedOption = $(this).val();
						container.update();
					});
					options.forEach(function(s) {
						var option = $("<option></option>").attr("value", s).text(s).appendTo(editor);
						if( s == node.data.selectedOption ) option.attr("selected", "selected");
					});						
					container.append(editor);
				}		

				var title = data.title;
				data.updateNode = function(node) {					
					node.setTitle(node.isSelected() ? title + " (Group by " + node.data.selectedOption + ")" : title);
				}
			}
			
			function setFieldsEditor(data) {	
				data.setEditor = function(container, node) {
					container.chrome(true);
					if( node.data.fieldType == "event" ) {
						$("<label><input type='checkbox' /> All events</label>")
							.appendTo(container)
							.find("input").change(function() {								
								node.data.allEvents = $(this).is(":checked");
								container.update();
							});
					}
				
					$("<div></div>").text("Include fiels (separate with line breaks):").appendTo(container);
					var editor = $("<textarea rows='8'></textarea>").blur(function() {
						node.data.customFields = $(this).val().match(/[^\r\n]+/g) || [];
						container.update();
					});	
										
					editor.text(node.data.customFields.join("\n"));															
					container.append(editor);
				}

				var title = data.title;
				data.updateNode = function(node) {
					var contextTitle = title;
					if( node.data.fieldType == "event" ) {
						contextTitle = title.replace("{0}", node.data.allEvents ? "Events" : "Goals");
					}
					
					if( node.isSelected() && node.data.customFields ) {
						contextTitle += " (" + node.data.customFields.length + " custom field" + (node.data.customFields.length > 1 ? "s" : "") + ")";
					}
					
					node.setTitle(contextTitle);
				}
			}
			
			function addEventsTable(table, node, name) {				
				name = name || "{0}";
				var allEvents = node.data.allEvents;
				var eventsTable = { name: name.replace("{0}", allEvents ? "Events" : "Goals"), fields: []};
				
				var eventField = {labels: {}};
				node.data.customFields.forEach(function(field) {
					var name = field.replace(/[@ ]/gi, "");
					eventField.labels[name] = field;
				});
				eventsTable.fields.push({"event": eventField});
				eventsTable.fields.push("facts");

				table.tables = table.tables || [];
				if( allEvents ) {
					table.tables.push({"events": eventsTable});						
				} else {
					table.tables.push({"goals": eventsTable});						
				}
				
			}
			
			function findComponent(list, key) {				
				for( var i = 0; i < list.length; i++ ) {
					for( var k in list[i] ) {
						if( k == key ) return list[i][k];
					}
				}				
			}
			

			var dimensionTemplate = "{82969FF1-15FA-4CDC-8CA4-5204A6ADD761}";
			
			var databaseUri = new _sc.Definitions.Data.DatabaseUri("master");
			var database = new _sc.Definitions.Data.Database(databaseUri);
			
			var _this = this;
			database.query("//*[@@id='{FBF255C0-72A2-4E76-A83D-633B852D82E7}']//*", function (items, totalCount, result) {
				var treeItems = foldItems(items);
								
				var xaItems = (function project(items) {					
					return items.map(function(item) {
						var isDimension = item.$templateName == "Dimension";						
						var xaPath = item.$path.replace(/^.*Dimensions\//gi, "");						
						return {							
							title: item.$displayName, 
							isFolder: item.children.length, 
							xaDimension: true,
							xaPath: xaPath,
							sortOrder: X.xaDimensionConfig.sortOrder[xaPath],
							key: item.itemId,
							icon: item.$icon,
							hideCheckbox: !isDimension,
							children: project(item.children)};
					});
				})(treeItems);
												
				var dynaItems = [];					
				dynaItems.push(createDimension("Date", "DATE", function(data){
					data.select = true; 
					data.selectedOption = "Date";					
					
					data.updateJob = function(table, data) {
						table.fields.push({"date": data.data.selectedOption});
					}
					
					setDropDownEditor(data, ["Year", "Quarter", "Month", "Date"]);
					
					return data;}));
				dynaItems.push(createDimension("Time", "TIME", function(data) {
					data.updateJob = function(table, data) {
						table.fields.push({"time": data.data.selectedOption});
					}
				
					data.selectedOption = "Hour";
					setDropDownEditor(data, ["Quarter", "Hour", "Minute"]);
					return data;
				}));
				
				dynaItems.push(createFolder("Experience Analytics", "XA_ROOT", function(data) {
					data.addClass += " dynatree-separate";
					data.expand = true;
					data.children = xaItems;
					return data;
				}));
				
				dynaItems.push(createFolder("Other", "XA_OTHER", function(data) {				
					data.addClass += " dynatree-separate";					
					data.children.push(createDimension("Pages", "PAGES", function(data) {
						data.selectWithChildren = true;		
						data.customFields = ["@DisplayName", "@TemplateName"];						
						data.fieldType = "page";
						setFieldsEditor(data);			
						
						data.select = false;
						data.children.push(createDimension("{0} per page", "PAGES_GOALS", function(data){
							data.customFields = ["@DisplayName", "Goal Facet 1", "Goal Facet 2", "Goal Facet 3", "Goal Facet 4"];
							data.fieldType = "event";
							setFieldsEditor(data); 	

							data.updateJob = function(table, node) {
								var t = findComponent(table.tables, "pages");
								if( t ) {
									addEventsTable(t, node, "Page{0}");
								}													
							};							
							return data;
						}));
						
						data.updateJob = function(table, node) {
							var pagesTable = { name: "Pages", fields: []};
							var pageField = {labels: {}};
							node.data.customFields.forEach(function(field) {
								var name = field.replace(/[@ ]/gi, "");
								pageField.labels[name] = field;
							});
							pagesTable.fields.push({"page": pageField});
							pagesTable.fields.push("facts");
							
							table.tables.push({"pages": pagesTable});
						};
						
						return data;
					}));
					
					data.children.push(createDimension("{0} per visit", "GOALS", function(data){
						data.customFields = ["@DisplayName", "Goal Facet 1", "Goal Facet 2", "Goal Facet 3", "Goal Facet 4"];
						data.fieldType = "event";
						setFieldsEditor(data);
						data.select = false;
						data.updateJob = function(table, node) {
							addEventsTable(table, node, "{0}");											
						};
						
						return data;}));
					
					data.children.push(createDimension("Visit ID (Export every visit separately)", "VISIT", function(data){
						data.select = false; 								
						data.updateJob = function(table, data) {
							table.fields.push("visit");
						}							
						data.addClass = "dynatree-separate";
						return data;
					}));
					
					data.children.push(createDimension("Contact ID (Unique visitors)", "VISIT", function(data){
						data.select = false; 								
						data.updateJob = function(table, data) {
							table.fields.push("contactid");
						}													
						return data;
					}));	
					
					return data;
				}));
				
				var nodeSettings = $("<div class='dimension-settings'></div>")
					.appendTo(_this.$el);					
													
				nodeSettings.css("position", "absolute").hide();
				nodeSettings.chrome = function(toggle) {
					nodeSettings.toggleClass("dimension-settings-chrome", toggle);					
				}
				nodeSettings.update = function(node) {
					var node = node || $(".xa-tree").dynatree("getActiveNode");		
					if( node ) {					
						if( node.data.updateNode ) node.data.updateNode(node);
						nodeSettings.position({my: "left+10px top-10px", at: "right top", of: $(node.span)});					
					}
					updateJob();
				}
				
				$(document).mousedown(function(e) {					
					if( !_this.$el.has(e.target).length ) {
						var activeNode = $(".xa-tree", _this.$el).dynatree("getActiveNode");						
						if( activeNode ) {
							activeNode.deactivate();
						}
					}
				});
														
				$(".xa-tree", _this.$el).dynatree({
						children: dynaItems, 
						debugLevel: 0, 
						checkbox: true,
						clickFolderMode: 1,
						onActivate: function(node) {
							if( node.data.setEditor ) {	
								nodeSettings.chrome(false);							
								nodeSettings.empty().show();
								node.data.setEditor(nodeSettings, node);								
								nodeSettings.update();														
							}							
						},
						onDeactivate: function(node) {
							nodeSettings.update();
							nodeSettings.hide();
						},
						onClick: function(node, ev) {							
							if( !$(ev.target).is(".dynatree-expander") ) {
								if( !node.isExpanded() ) {
									node.expand();								
								}					
							}
						},
						onDblClick: function(node, ev) {
							node.toggleSelect();
						},
						onBlur: function(node) {
							updateJob();						
						},
						onKeydown: function(node, ev) {							
							if( ev.keyCode == 32 ) {
								node.toggleSelect();
							}							
						},
						onSelect: function(select, node) {													
							if( select && !node.getChildren() ) node.activate();
							
							if( node.data.updateNode ) {								
								nodeSettings.update(node);
							}
							
							if( select && node.parent && node.parent.data.selectWithChildren ) {
								node.parent.select();
							}							
							if( !select && node.data.selectWithChildren ) {								
								node.getChildren().forEach(function(child) { child.select(false);});
							}
						},
						onCreate: function(node) {							
							if( node.data.updateNode ) node.data.updateNode(node);
						}
					});				
					
				updateJob();
			});			
		}
	});
	
});