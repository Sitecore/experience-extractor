Using Experience Extractor
=

Data is extracted from xDB by posting _job specifications_ to Experience Extractor's REST API. The default endpoint is `/sitecore/experienceextractor/jobs`.

To get started you can use the `/sitecore/admin/experienceextractor/shell.aspx`that provides syntax highlighting and handles requests to the API. 

As of version 0.2.x you can also open "Experience Extractor" from Sitecore's launch pad. This provides a [user interface](https://raw.githubusercontent.com/wiki/Sitecore/experience-extractor/resources/02x-ui.png) around the shell to assist building jobs. 

> The shell in the UI uses [YAML](http://yaml.org/) since it's more compact than JSON, and adds the "msaccess" post processor to jobs. This requires [Microsoft.ACE.OLEDB.12.0](http://www.microsoft.com/en-us/download/details.aspx?id=13255) to be installed on the server (typically the x64 version). 



## Job specifications
A job specification consists of

1. A data source, optionally with filters
2. A specification of the data to extract
3. Optionally one or more post-processors that transform the raw CSV files to more convenient formats

Job specifications are represented in JSON. A very simle one can look like this
```javascript
{
	"source": {
	    "xdb": { 		                    // Load data from xDB
	          
	        "filters": [{"limit": 1000}] 	// Limit the number of
									        // visits to 1000
		}
	},
	
	"mapper": {
		"tables": [		
         {
            "name": "VisitsByMonth",  // Create a table with the name 
							          // "VisitsByMonth" in the output
            "fields": [
               {"date": "month"},      // Aggregate visits by month
						               
	           "facts"	               // Add the columns Visits, Value, 
							           // Bounces, Conversions, TimeOnSite, 
							           // PageViews and Count
							           // for the aggregated rows
            ]
         }
      ]
   }
}
```

If you run this job in the shell you will get a ZIP-file containing the two CSV files `VisitsByMonth.txt` and `StartDateTime.txt` with this schema

![Schema of the exported files](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Sample01.png?raw=true)

The table "VisitsByMonth" has more columns than the two "fields" specified in the job (`date` and `facts`), and a date dimension table has been added. This is a central concept in Experience Extractor, where a "field" means "Add columns and tables related to this information". The `date` field adds a date dimension and associates it with a foreign key (StartDateTimeId) in the main table. Here "Month" is specified to aggregate visits per month, but other options are "Year", "Quarter" and "Day".

The date fields can also be included in the main table by setting `inline` attribute in the `date` field
```javascript
...
	"fields": [
               {"date": {"resolution": "month", "inline": true}},
               "facts"
          ]
...
```

This extract one file with this schema

![Date fields inlined](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Sample02.png?raw=true)


The column VisitsByMonthId" is a hash key for the aggregated rows that is automatically generated from the dimensions in the table. Since the only dimension is "StartDateTime", this could be used for key as well 

```javascript
{"date": {"resolution": "month", "inline": true, "key": true}}
```

but the hash keys are convenient when multiple dimensions are added, as in the next examples


### Experience Analytics
Let’s add two dimensions from Experience Analytics (XA) and export all visits from January 2015
```javascript
{
   "source": {
      "xdb": {
         "filters": [
	         {"daterange": {
		         "start": "2015-01-01Z", 
		         "end": "2015-02-01Z"}
		     }
		 ]
      }
   },
   "mapper": {
      "tables": [
         {
            "name": "VisitsByMonth",
            "fields": [
               {"date": {"resolution": "month"}},
               {"xa": "Visits/By Country"},
               {"xa": "Visits/By Campaign"},
               "facts"
            ]
         }
      ]
   }

```
Now the schema looks like this. Each Experience Analytics, `xa`, dimension adds two fields; the dimension’s "key" and its label. The label is the friendly name, for example the label for "DK" is "Denmark" in the _By Country_ dimension.

![Experience Analytics dimensions included](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Sample03.png?raw=true)

Dimensions from Experience Analytics can be specified either as with dimension's ID, or a path relative to `/sitecore/system/Marketing Control Panel/Experience Analytics/Dimensions/` in the master database

Instead of using the `limit` filter the `daterange` filter is used filter the extracted data by dates. The "Z"s ending the dates mean that they are UTC dates to ignore local time zone. If the "Z" had not been there, running the above job in CET (GMT + 1) would give all visits from 2014-12-31 23:00 to 2015-01-31 23:00.




### Rules based filters

As with segments in Experience Analytics, rules based filters can be used to limit the extracted data in Experience Extractor.
Filters are defined in `/sitecore/system/Marketing Control Panel/Experience Analytics/Filters` and can be referenced either with their name relative to this path or by their ID.

```javascript
"source": {
      "xdb": {
         filters: [
            {"rule": "Branded organic search"}
         ]
      }
   }
```



### Dimension tables
Experience Analytics dimensions can be combined in separate dimension tables. This reduces the size of the output and can be used to logically group different dimensions when data is analyzed. In the specification below a `dimension` field is added to extract a dimension table named "Geo" with the three Experience Analytics dimensions "Country", "Region" and "City". This makes sense because there is a hierarchical relationship between these. A hash key is generated for each "Geo" row and this key is used as reference in the main table (VisitsByMonth)

```javascript
"fields": [
   {"date": {"resolution": "month"}},
   {"dimension": {
     "name": "Geo",
     "fields": [
        {"xa": "Visits/By Country"},
        {"xa": "Visits/By Region"},
        {"xa": "Visits/By City"}
     ]
   }},
   {"xa": "Visits/By Campaign"},
   "facts"
]

```

![Geographical Experience Analytics dimensions in separate table](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Sample04.png?raw=true)


### Child tables
Some Experience Analytics dimensions generate more than one row per visit, for example "Pages/By Page URL" emits a row for each distinct page visited. In this case they are added as tables instead of fields in the job as in this example:

```javascript
mapper: {
    "tables": [
         {
            "name": "VisitsByMonth",
            "fields": [
               {"date": {"resolution": "month"}},
               {"dimension": {
                 "name": "Geo",
                 "fields": [
                    {"xa": "Visits/By Country"},
                    {"xa": "Visits/By Region"},
                    {"xa": "Visits/By City"}
                 ]
               }},
               {"xa": "Visits/By Campaign"},
               "facts"
            ],
            
            //Add a child table from the By Page Url dimension
            "tables": [
               {"xa": "Pages/By Page Url"}
            ]
         }
      ]
}
```

This adds a child table to VisitsByMonth that aggregates rows for each PageUrlKey. The hash key from VisitsByMonth is used as parent key.

![Experience Analytics dimensions added as child table](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Sample05.png?raw=true)

From this export we can for instance analyze "Most frequently visited pages by country and campaign" for different time periods. This is one way that could look like in a pivot table in Excel with a slicer for country and a timeline

![Experience Analytics dimensions added as child table](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Pivot01.png?raw=true)



### Export to database
When working in Excel the raw CSV files from the output can be imported with Power Pivot, but it is simpler to connect to a database. For this reason Experience Extractor can generate a database with the results as part of a job. 

Two options exist:

-	`msaccess`: Microsoft Access. Adds a Microsoft Access database to the output with the tables extracted including their relations. Note that a 64 bit version of the Microsoft.ACE.OLEDB.12.0 provider must be installed on the server (if it is x64)
-	`mssql`: Microsoft SQL Server. Creates a database with a schema matching the extracted data and populates it. For this a SQL Server instance that can be accessed by the Sitecore server needs to exist, and the user specified in the connection string must have permission to create databases. 


To generate a database as part of a job add one of these post-processors:
```javascript

"postprocessors": [
	  //Microsoft Access
      "msaccess", 
      
      //Microsoft SQL Server
      {"mssql": { 
 
 //Connection string or name of connection string in ConnectionStrings.config
            "connection": "Server=.\\SQLEXPRESS;User Id=sa;Password=(password)",

//Name of database to create. If it already exists it is dropped and recreated
            "createDatabase": "Example" 
      }}
   ]

```


### Date and time dimensions
Date and time dimensions contain extended information about date/time properties in xDB to analyze data by month, weekday etc. Time dimensions always contain all hours from 00:00 to 24:00 and date dimensions contain all dates from the oldest to the most recent date in the extracted data. This is handy for time series plots since dates with no visits are also available.
Date and time dimensions are added by including the fields `date` and/or `time` in a job specification.

Both have a resolution option.
`date`: Year, Quarter, Month, Date
`time`: Hour, Quarter, Minute

The fully expanded tables have these fields:

![Experience Analytics dimensions added as child table](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Sample06.png?raw=true)

To limit the extracted data to a specific date range use the `daterange` filter in the data source:

```
"xdb": {
         "filters": [{"daterange": {
	         "start": "2015-01-01Z", 
	         "end": "2015-02-01Z"
	     }}]
      }
```
If start or end is omitted the filter will give all visits "since start" or "until end". Start is inclusive and end is exclusive, i.e. StartDateTime >= start and StartDateTime < end.


### Sampling
To reduce the amount of extracted data a random sample can be made with the `sample`filter. Trends and patterns in such samples will typically be representative for the population but require less processing power and storage space. 

```javascript
"filters": [
            {"sample": .5} //Extract a random sample with 50% of the interactions
         ]
```
Sampling is based on IDs stored in xDB so the same results will be extracted if the same job specification is run multiple times (on the same data). The offset property can be used to get another sample.

```javascript
"filters": [
            {"sample": {percentage: .1}}, //"First" 10 %
            {"sample": {percentage: .1, offset: .1}} //"Next" 10 %
	etc..
         ]
```

Sampling can be combined with other filters


### Localization
If month names, Experience Analytics dimension labels, item properties etc. are needed in another language than English, specify the attribute "labels" in the job specification

```javascript
{
   "labels": " fr-CA",

   "source": "xdb",
   "mapper": ...
}
```


## Other features
Apart from date and time and dimensions from Experience Analytics, Experience Extractor also allows:

-	Access to all properties of VisitData, PageData and PageEventData
-	Field value lookup in Sitecore’s item database
-	Split facts and tables by what happended before and after an MV test
-	Co-occurrence and link matrices based on pages, events and goals

A complete list of all options available is available by opening /sitecore/experienceextractor/jobs/metadata in a browser


### Raw property values and data from the item database
In some cases it may be convenient to include property values from `VisitData`, `PageData` or `PageEventData` directly or to include information from Sitecore’s item database. This can be display name, name of data template or the value from one or more fields. This allows to analyze and aggregate visits by data from the CMS, for example, tags or other properties that have been applied to pages.

 `visit`, `page` and `event` accesses raw properties on the current `VisitData`, `PageData` or `PageEventData` in the processing scope.

For tables `pages`, `events` and `goals` creates child table for these. If `events` or `goals` are added to a table with visits, all events or goals in a visit are listed regardless of page.

The syntax to access properties and fields is "Property on object[/Field name in item database]".

For example
`"GeoData.Country"` will return a visit’s country code
`"Item.Id/@DisplayName"` will look up the item with the ID `Item.Id`  from `PageData` and return its display name
`PageEventDefinitionId/Goal Facet 1` will return a goal's value in the field "Goal Facet 1"
 
In this example visits, pages and goals are extracted:
```javascript
{
   "labels": "en-US",
   "source": "xdb",
   "mapper": {
      "tables": [
         {
            "name": "Visits",
            "fields": [
               "date",
               //Include the raw value of GeoData.Country
               {"visit": "GeoData.Country"},
               "facts"
            ],
            "tables": [
               //Include pages for each visit
               {"pages": {
                 name: "Pages",
                 fields: [
                    //The index of the page in the visit
                    "index",
                    {"page": {
                       //Include display name, template name and the value of the field "Title"
                      labels: {"Name": "@DisplayName", Template: "@TemplateName", Title: "Title"}
                    }},
                    //Include facts
                    "facts"
                  ],
                  tables: [
                     //Include goals for each page
                     {"goals": {
                        name: "Goals",
                        fields: [
                           //The index of the event relative to page
                           "index",
                           //Index of the event relative to visit
                           {"index": {name:"IndexInVisit", scope:-2}},
                           {"event": {labels: {Name: "@DisplayName"}}},
                           "facts"
                        ]
                     }}
                  ]
               }}   
            ]
         }
      ]
   }   
}
```

![Experience Analytics dimensions added as child table](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Sample07.png?raw=true)


### A/B and Multi-variate tests
This example demonstrates how to get detailed information about an MV Test made in Sitecore.

```javascript
"tables": [
    {
       "name": "VisitsByMonth",
       "fields": [
          {"date": {"resolution": "month"}},
          {"dimension": {
            "name": "Geo",
            "fields": [
               {"xa": "Visits/By Country"},
               {"xa": "Visits/By Region"},
               {"xa": "Visits/By City"}
            ]
          }},
          {"xa": "Visits/By Campaign"},
          {"mvtest": {
                testid: "{6FA234D0-45AB-457A-A5D5-884ECD3E6023}",
                fields: [{"facts": ["Value", "PageViews"]}]
          }},
          {facts: ["Visits"]}
       ],
       
       "tables": [
          {"split": {
             splitter: {"mvtest":"{6FA234D0-45AB-457A-A5D5-884ECD3E6023}"},
             table: {"goals": {
                name: "Goals",
                fields: [
                   {"event": "PageEventDefinitionId/@DisplayName"},
                   {"facts": ["Visits", "Count", "Value"]}
                ]}
             }
          }}
       ]
    }
 ]
```

![Experience Analytics dimensions added as child table](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Sample08.png?raw=true)

The test was a simple A/B test, and the variation exposed to the visitor is contained in "MvTestVar1" and "MvTestVar1Name". The facts "ValueBefore", "ValueAfter" and "ValueTotal" contain the aggregated engagement value for each variation before, after the test and in total repectively. The "winner" of this test will be the variation with the highest "ValueAfter" value, but since campaign and geographical information are included, the extracted data can be used to see if different visitors prefer different variants. If so this would suggest that a personalization rule should be added and tested.
In the same way the GoalsBefore and GoalsAfter tables contain the events that happened before and after the test. Dividing the value of "Visits" from GoalsAfter with "Visits" in VisitByMonth gives the conversion rate for each goal with the option to slice and dice based on campaign, date and geographical information.


### Matrices and paths
Adding a `matrix` component in `tables` generates adjacency lists that can be interpreted as a matrix. This can be used to answer the questions "which pages lead to other pages?", "which events occurred together" or be used in more advanced scenarios to find similar pages based on PCA or calculate page ranks.
Below is an example of a table that shows the number of visits where goals occurred together

![Experience Analytics dimensions added as child table](https://github.com/Sitecore/experience-extractor/blob/master/doc-resources/Pivot02.png?raw=true)

The diagonal is the number of visits where the goal was converted, and the off-diagonals are where they occurred together.

The job specification for this is:
```javascript
{   
   "source": "xdb",
   "mapper": {
      "tables": [
         {
//Name and fields are not specified in the main table since we don't need it
						
            tables: [
//Goals that occurred together
               {"matrix": {
                  name: "GoalsTogether",
                  type: "cooccurrence",
                  select: "goals",
                  fields: [{"event": "PageEventDefinitionId/@DisplayName"}],

 //Count from scope -1 counts the distinct number of visits. TotalCount includes when an goal was triggered multiple times in the same visit
                  commonFields: [{"count":{"scope":-1}}, {"count":"TotalCount"}]
               }},

//Navigation between adjacent pages in visits
               {"matrix": {
                  name: "PageNavigation",
                  type: "links",
                  select: "pages",
                  fields: [{"page": "Item.Id/@DisplayName"}]
               }}
            ]
         }
      ]
   }
}
```
Dimensions can be included to filter the exported matrices by campaigns, date/time etc. but beware that including many dimensions can potentially explode the number of rows.


### All options / Documentation from the API
All options
A complete list of all options available in Experience Extractor is available by opening /sitecore/experienceextractor/jobs/metadata in a browser.

The general syntax for a component in a job specification is


#### Shorthand
```javascript
"key"
```
This will include the component with all attributes set to their default value


#### Shorthand with main parameter
```javascript
{"key": "value of default parameter"}
```

This will include the component with the  main parameter set to the specified value. The main parameter is the one with `IsMainParameter` = `true`, if any.


#### Full specification
```javascript
{"key": {
   attribute1: value,
   attribute2: value,
   ...
}}
```
Each registered component is provided by /sitecore/experienceextractor/jobs/metadata with the following information:

-	Key: The key to use in a job specification, for example "xdb" or "facts"
-	Application: Where it can be used IDataSource, IFieldMapper, ITableMapper, TableDefinition or ISplitter
-	Name
-	Description
-	Assembly/FactoryType: Where the component is defined
-	Parameters
	- Name: The attribute name in job specifications
	- Type: The expected type
	- Description
	- DefaultValue: The default value if omitted
	- IsMainParameter: If true, this parameter is set in the short hand notation {key: "(value)"}

For example `xdb` is stated as follows:
```javascript
{ 
	"Assembly": "ExperienceExtractor.MongoDb", 
	"Key": "xdb", 
	"FactoryType": "ExperienceExtractor.MongoDb.MongoDbVisitAggregationContextSource+Factory", 
	"Application": "IDataSource", 
	"Name": "MongoDB xDB connection", 
	"Description": "Loads IVisitAggregationContexts from xDB limited by the filters specified", 
	"Parameters": [ 
	{ 
	"Name": "Connection", 
	"Type": "String", 
	"Description": "MongoDB connection string or name of connection string defined in <connectionStrings />", 
	"DefaultValue": "The connection string defined in Experience Extractor's config file", 
	"IsMainParameter": true 
	}, 
	{ 
	"Name": "Filters", 
	"Type": "IEnumerable<IDataFilter>", 
	"Description": "Filters to limit the visits to extract", 
	"DefaultValue": null, 
	"IsMainParameter": false 
	}, 
	{ 
	"Name": "Fields", 
	"Type": "String", 
	"Description": "Limit the fields returned from MongoDB for faster results. Note that entities will only be partially hydrated with the subset of fields specified which can give misleading results if fields expecting other values are included.", 
	"DefaultValue": null, 
	"IsMainParameter": false 
	}, 
	{ 
	"Name": "Index", 
	"Type": "String", 
	"Description": "Specific index to use to optimize extraction from MongoDB. Use the value '$natural' to scan all documents in MongoDB in insert order rather than loading them with an index. In situations where database size vastly exceeds available RAM this can produce faster results since accessing documents in index order may load a lot of pages in and out of RAM.", 
	"DefaultValue": null, 
	"IsMainParameter": false 
	} 
	] 
	}
```


## REST API

A job is started by posting a job specification in JSON to
```HTTP
POST /sitecore/experienceextractor/jobs
```

The status of a job is obtained from
```HTTP
GET /sitecore/experienceextractor/jobs/{id}
```

When a job has completed a ZIP file with the resulting files can be downloaded from
```HTTP
GET /sitecore/experienceextractor/jobs/{id}/result
```

A job is cancelled with
```HTTP
DELETE /sitecore/experienceextractor/jobs/{id}
```

The list of jobs running or completed since the server started is listed at
```HTTP
GET /sitecore/experienceextractor/jobs
```

The list of available options in job specifications is listed at
```HTTP
GET /sitecore/experienceextractor/jobs/metadata
```

Job status is returned in this format
```json
{
  "Id": "Job ID",
  "Created": "Created date",
  "Ended": "Ended date (null if processing)",
  "ItemsProcessed": "The number of items processed",
  "Progress": "Percentage of estimated number of items processed. Can be null if unknown",
  "Status": "Job status",
  "StatusText": "Description related to current job status",
  "SizeLimitExceeded": "true if job was ended prematurely due to size limit constraints",
  "Url": "The url of this job status",
  "ResultUrl": "When a job has successfully completed, the URL to download the results",
  "Specification": "The JSON job specification",
  "LastException": "If a job failed this will contain a description of the exception"
}
```

A job can have these statuses:

Pending
:	Job has not been started

Preparing
:	Count is being estimated

Running
:	Items from the data source are processed

Merging
:	Items have been processed and intermediate results written to disk are being merged

PostProcessing
:	Post processors are running on the extracted data

Completing
:	Used for event hooks that runs just before completion

Completed
:	Job has completed

Failed
:	Job failed. 

Canceled
:	Job was cancelled


The URL to the API can be changed in configuration be changing the `apiRoute` attribute in `App_Config\Include\ExperienceExtractor\ExperienceExtractor.config`


## Integrating Sitecore xDB data in external systems
The REST API of Experience Extractor can be used to integrate Sitecore data in other systems. Typically this will involve dynamically setting the `daterange` filter in a script and some external scheduling mechanism.
Rather than polling the REST API to find out when a job has completed it may convenient to include
```javascript
export: {ping: "http://triggerme.com/?id={id}&status={status}"}
```

in the job specification. The specified ping URL will be called when a job has ended (completed, failed or has been cancelled).

Another option is to extend the ```ITableDataPostProcessor```interface to do something with the data as part of the job. For instance the ```SqlExporter``` implements this interface to create a database and load the data. In a similar way some other external database can be updated with the data, or the data can be uploaded to some external service.

# Custom data / Extending Experience Extractor
Experience Extractor can be extended with domain specific data and to support custom need.

Example scenarios:

- A "Put in basket" event is registered during the visit with the basket value and number of items as custom data. These numbers are added as facts that can be combined with Experience Analytics dimensions and used to evaluate MV tests. `IFieldMapper`

-	An order with order lines has been added as custom data on visits where a purchase was made. An option to include child tables with the items purchased is added to treat these items in the same way as pages and goals. (`ITableMapper` and `IFieldMapper` for custom item fields)

-	"Testing without a test". What happened before and after a custom criteria (spend 4 minutes in support section, read article, put first item in basket etc.).  If needed a "split" can have more than two states (e.g. Before entering support section, Before spending 4 minutes, after). `ISplitter`

-	Extracted data is compiled into a format easier to use with my preferred application (Tableau, Gephi, etc) (`ITableDataPostProcessor`)


### Registering a field for a simple custom value
This example adds `basketvalue` that sums the "BasketValue" from `CustomValues` in "Put in basket" events that happened during visits.

This enables basket value to aggregated over dimensions just like `facts` in a job specification like:
```javascript
...
"tables": [
      {
         "name": "VisitsWithBasketValue",
         "fields": [
            {"date": {"resolution": "date"}},
            {"xa": "Visits/By Country"},
            {"xa": "Visits/By Campaign"},
            "basketvalue",
            "facts"
         ]
      }
   ]
...
```

The implementation is

```c#
//This attribute registers the factory and provides documentation in /sitecore/experienceextractor/jobs/metadata
    [ParseFactory("basketvalue", "Basket value", description: "The basket value at the end of the visit")]
    public class BasketValueFactory : IParseFactory<IFieldMapper>
    {
        //The item ID of the "Put in basket" event
        public static Guid BasketEventId = Guid.Parse("{5E31CCE2-78F7-4C16-83E2-E701B7B14AD5}");

        //This is the factory method.
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            //SimpleFieldMapper uses a lambda expression to get the value
            return new SimpleFieldMapper("BasketValue", scope => GetBasketValue(scope.Current<IVisitAggregationContext>().Visit),
                valueType: typeof(decimal), //Basket value is a decimal
                fieldType: FieldType.Fact); //The field is a fact. This means that values summed for each aggregated row
        }

        //Sums the "BasketValue" in the custom values dictionary for all "Put in basket" events
        private static decimal GetBasketValue(VisitData visit)
        {
            if (visit == null) return 0;
            var sum = 0m;
            //Find all "Put in basket" events in the visit
            foreach (var basketEvent in visit.Pages.SelectMany(page => page.PageEvents)
                .Where(ev => ev.PageEventDefinitionId == BasketEventId))
            {
                object value;
                if (basketEvent.CustomValues.TryGetValue("BasketValue", out value))
                {
                    sum += Convert.ToDecimal(value);
                }
            }

            return sum;
        }
    }
```

### Making fields available for job specifications
The assembly including the extensions must be registered in the configuration file before they can be used. Do this by adding the assembly name in the `<parsing>` section, e.g.

```xml
<parsing>
	<assembly>ExperienceExtractor.Components</assembly>
	<assembly>ExperienceExtractor.MongoDb</assembly>
	
	<assembly>Acme.MyBasketValueExtensions</assembly>

</parsing>

```
The parse factories in the assembly are automatically discovered from the `ParseFactoryAttribute` on application startup


### Adding facts with domain specific calculations
In the previous example basket value was added as a fact for an entire visit, and now assume the option to analyze basket value per page and goals is also needed. In this example options are provided to give the basket value and number of items in the basket:

- At the end of the visit
- At the time a page was visited
- The change in basket value and item count when a page was visited

In this example the `ParseState` is accessed to get values from a job specification and the `ProcessingScope` is used to determine if the current item being processed is a visit, a page or an event. In this way the same `basketfacts` can be used both for visits, pages and events, and provide information based on the context where it is applied.

Note how `ParseFactoryParameterAttribute`s are used to provide documentation.

```c#
[ParseFactory("basketfacts", "Basket facts", description: "Basket value and number of items in basket"),        
        ParseFactoryParameter("Calculation", typeof(CalculationMethod), 
@"How to calculate the facts. Options are:
    Current: The current value relative to the page or event being processed.
    Delta: The change in basket value relative to the page or event being processed
    EndOfVisit: The basket value at the end of the visit regardless of page or event being processed", defaultValue: "Current", isMainParameter: true),
        
    ParseFactoryParameter("Prefix", typeof(string), "Prefix column names with this. Useful if multiple basketfacts are included in the same table with different calculation methods")]

    public class BasketFactsFieldFactory : IParseFactory<IFieldMapper>
    {
        /// <summary>
        /// The ID of the "Put in basket" event item in Sitecore
        /// </summary>
        public static Guid BasketEventId = Guid.Parse("{5E31CCE2-78F7-4C16-83E2-E701B7B14AD5}");

        
        /// Returns a field mapper for the two fact fields "BasketValue" and "ItemsInBasket"
        public IFieldMapper Parse(JobParser parser, ParseState state)
        {
            //Get the calculation method
            var calculation = state.TryGet("Calculation", CalculationMethod.Current, mainParameter: true);

            //Get the column prefix
            var prefix = state.TryGet<string>("Prefix");

            //Add this prefix to column names
            state = state.Prefix(prefix);

            //Use FieldMapperSet.Inline to return the two field mappers as a single field mapper
            return FieldMapperSet.Inline(                
                
                //state.AffixName adds pre and postfixes for field names set in parent scopes
                new SimpleFieldMapper(state.AffixName("BasketValue"),
                    scope => SumValues(scope, calculation, "BasketValue"),
                    valueType: typeof (decimal), fieldType: FieldType.Fact),

                new SimpleFieldMapper(state.AffixName("ItemsInBasket"),
                    scope => (int) SumValues(scope, calculation, "ItemCount"),
                    valueType: typeof (int), fieldType: FieldType.Fact)
                );
        }

        decimal SumValues(ProcessingScope scope, CalculationMethod method, string key)
        {
            if (method == CalculationMethod.Delta)
            {
                //TryGet is an extension method that will return 0, i.e. default(typeof(decimal)), if the object it is invoked on is null
                return scope.Current<PageData>().TryGet(page => GetBasketValue(page, key));
            }
            
            var visit = scope.Current<IVisitAggregationContext>().Visit;
            //If there for some reason i not visit in scope, return 0
            if (visit == null) return 0;

            //The current page being processed
            var referencePage = scope.Current<PageData>();
            if (referencePage == null || method == CalculationMethod.EndOfVisit)
            {
                //If no page is being processed, accumulate values for the visit's last page
                referencePage = visit.Pages.Last();
            }

            var sum = 0m;
            foreach (var page in visit.Pages)
            {
                sum += GetBasketValue(page, key);
                //Stop summing values when the reference page is met
                if (page == referencePage) break;
            }

            return sum;
        }

        //Searches for "Put in basket" events on a page and returns the custom value with the key specified if found
        decimal GetBasketValue(PageData page, string key)
        {
            var basketEvent = page.PageEvents.FirstOrDefault(e => e.PageEventDefinitionId == BasketEventId);
            if (basketEvent != null)
            {
                object value;
                if (basketEvent.CustomValues.TryGetValue(key, out value))
                {
                    return Convert.ToDecimal(value);
                }                
            }

            return 0m;
        }

        public enum CalculationMethod
        {
            Current,
            Delta,
            EndOfVisit
        }       
    }    
```

With this the following is now possible:
```javascript
...
"tables": [
      {
         "name": "VisitsWithBasketValue",
         "fields": [
            {"date": {"resolution": "date"}},
            {"xa": "Visits/By Country"},
            {"xa": "Visits/By Campaign"},
            "basketfacts", //Basket value and number items in basket at the end of visit
            "facts"
         ],
			tables: [
				pages: {
					name: "Pages",
					fields: [
						{"page": "/@DisplayName"},
						"facts"
						//Aggregate basket value and number of items per page
						{"basketfacts": {"calculation": "current"}},
						//Add DeltaBasketValue and DeltaItemCount
						{"basketfacts": {"calculation": "delta", prefix: "Delta"}}
					]
				}
			]
      }
   ]
...
```