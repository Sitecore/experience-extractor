Experience Extractor
=================

Copyright 2015 Sitecore Corporation A/S

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.

For information about how to get started contributing to the project, read the FAQ in the Wiki (https://github.com/Sitecore/experience-extractor/wiki/Contributor-License-Agreement-FAQ)


About
-
Experience Extractor is a community tool for exporting experience data from Sitecore to:

- Analyze, present and share Sitecore data with external tools such as Excel, Power BI, R and Tableau
- Prepare and shape data for machine learning
- Integrate Sitecore Experience Data in other big data solutions


Experience Extractor runs data processing tasks (ETL) on Sitecore’s domain model, and exports data from xDB as tabular data. 

The dimensions, facts and rules based filters from Sitecore Experience Analytics can be used to specify the data to extract, and with Experience Extractor these dimensions can be correlated for multi-dimensional analysis, and data can be pre-aggregated to a desired date and time resolution (year, month, day, hour, minute) or exported for every visit.

This enables reuse of concepts from Sitecore's reporting when data is used externally. By executing the jobs in Sitecore context using Sitecore’s API rather than querying the underlying databases directly, Experience Extractor provides an option for data integrations that are more robust towards future upgrades of Sitecore, blends data from xDB and Sitecore’s item database, and enables reuse of custom logic for data stored in xDB.

Experience Extractor generates tabular data organized in a snow flake schema, and the main output is flat CSV files to be compatible with almost any data application. Data extracts are specified with modular building blocks and the output is pre-aggregated according to these with surrogate keys generated to relate the tables.

Options for generating SQL Server or Microsoft Access databases with the exported data are included, which allows the exported data to be loaded more easily to e.g. a Power Pivot data model complete with relations between tables with very few clicks.  Custom post processors that are run as part of an export job can be added to tailor the data to other applications.

Experience Extractor performs the necessary queries against Sitecore’s databases, and is designed to process data in parallel and offload data to disk if it becomes too big to fit in memory. In this way large amounts of data can be processed efficiently and a balance between processing speed and available hardware can be configured depending on needs.

Experience Extractor is exposed through a RESTful API where jobs are specified in an extensible JSON format.

##Compatibility
Experience Extractor has been tested with Sitecore XP 8.0, 8.1 and 8.2.

Experience Extractor requires direct access to Mongo DB and Sitecore's item database, and is currently not compatible with Sitecore xDB Cloud Service.

##Installation
Download and install [the package](https://github.com/Sitecore/experience-extractor/releases) on the server(s) where Experience Extractor will be available. 
To verify the installation succeeded open the URL /sitecore/admin/experienceextractor/shell.aspx

###xDB connection string
The connection string Experience Extractor uses to query MongoDB must be added to `App_Config/ConnectionString.config` before data can be extracted. The default name is "experienceextractor".

In a test environment the connection string can simply be the same as “analytics”, but in a production environment Experience Extractor should not read data from the primary instances in a MongoDB cluster since this may degrade the performance of Sitecore's operational data store. 

Instead, set up Experience Extractor to only read from secondary instances with `?readPreference=secondary` and optional tag preferences in the connection string. Please refer to the [MongoDB documentation](http://docs.mongodb.org/v2.6/core/read-preference/) or [this blog post](http://devops.com/blogs/mongodb-replication-pro-tips/) for details and implications.

###Security
By default only Sitecore administrators can use Experience Extractor. Specific roles and users can be added in the configuration file.
Please refer to `App_Config/Include/ExperienceExtractor/ExperienceExtractor.config` for details and other configuration options.

###Building from source
Sitecore binaries are not part of this distribution. You need to copy your own version of these to the “components” folder in the solution's root folder. NuGet package restore is used.



##How to use
Please refer to the [Wiki](https://github.com/Sitecore/experience-extractor/wiki/) for documentation on how to use Experience Extractor and examples.

<img src="https://drive.google.com/uc?export=download&id=0B0Vm6WIFt16zdlM0dVQ2b3JxZkE&authuser=0" width="720" height="405" />
