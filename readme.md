# Promethean API Test Runner

TODO:
command line options

1. Update a status report
    1. Add analysis on how many were successful and how many not on validation
1. 1. Compatibility 
we use UTF-32 to ensure maximum compatibility as the application has to support Chinese traditional and Chinese simplified. Traditional UTF-8 does not support it.


1. 1. when run exe display basic instance
1. Compatibility -> we use UTF-32 to ensure maximum compatibility as the application has to support Chinese traditional and Chinese simplified. Traditional UTF-8 does not support it.


## Purpose of the tool
- Create an API Test, and: 
    - Review the return.
    - Review and the database store (based on MS SQL database).
- Capture (Store) the test results to file.
- Compare with previous request.

Supported methods

 |Request Types |Supported  |
 |--            |--         |
 |GET           |Yes        |
 |PUT           |Yes        |
 |POST          |Yes        |
 |PATCH         |Yes        |
 |DELETE        |Yes        |


Modes for the implementation

This tool allows you to run the api calls in multiple modes
- FileCompare


### FileCompare
we compare only a response from the file

## Configuration fields

|Key|Required|Value|
|--|--|--|
|HeaderParam|no| Appends header information to the API requires|
|OutputLocation|Conditional|Location where captured logs are stored. Depends on ConfigMode = Capture or CaptureAndCompare|
|ResultsStoreOption|Yes|Required with possible values None, FailuresOnly, All|
|UrlBase|Yes | Location where to make api call to|
|UrlParam|Yes|Url param allows adding query parameters to url|

### UrlParam

#### Static binding
----
Populate the http request with a value to get a static parameter

```url
http://localhost:7055/WeatherForecast?urlkey=configKey
```
the config will look like this:

```c#
UrlBase = "http://localhost:7055",
UrlPath = "/WeatherForecast",
UrlParam = new List<Param>
            {
                new Param("urlKey", "configKey"),
            }
```

#### Data driven parameter

populate id with database

```url
http://localhost:7055/WeatherForecast?id=15
```

config to create the binding, mark the id from database in your sql query and use dbfields to capture the output

```c#
UrlBase = "http://localhost:7055",
UrlPath = "/WeatherForecast",
DBConnectionString = "<insert your connection string>",
UrlParam = new List<Param>{
    new Param("urlKey", "configKey"),
    new Param("id", "bindingId")
},
DBQuery = "select id as bindingId from dbo.sampleTable;",
DBFields = new List<Param>{
    new Param("bindingId", "bindingId"),
    new Param("fieldName", "fieldName")
},
```

this will map to param with this pattern

```url
http://localhost:7055/WeatherForecast?id={bindingId}
```

### ConfigMode types

|Type                   |ConfigValue|UseCase|
|--                     |--         |--|
|Run                    |1          |Runs the tests only and shows result as overview.|
|Capture                |2          |Runs the tests and capture the results. Process will fail in case the file already exists. |
|CaptureAndCompare      |3          |Calls APIs and store result. If file already exists then it wil also compare output with api result.|

~~|APICompare             |4 |Not implemented yet. Realtime compare. Compares the results of two APIs. Good for regression testing of APIs.|~~

### ResultsStoreOption

This has to be used with configuration ConfigMode min level capture

|Name                   |Numeric value| purpose|
|--                     |--           |--|
|None                   | 0           |Just run the tests|
|FailuresOnly           | 1           |Record only failures|
|All                    | 2           |Stores all results|


if while is being stored, the file name is part of output data
```bash
/WeatherForecast?urlKey=configKey&id=1 404 fail Results/request-1.json
```
    
#### RUN

takes the configuration and only execute a call against the api
Reports success to the api as

Example of the test:
```bash
api base url: http://localhost:7071/

1.  /Data		OK success
2.  /Data/1		OK success
```


## Setting up docker container as data source

Note: out of date needs more columns to support test cases

```bash
docker pull mcr.microsoft.com/mssql/server:2022-latest
```


```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<YourStrong@Passw0rd>" -p 1433:1433 --name sql1 --hostname sql1  -d  mcr.microsoft.com/mssql/server:2022-latest
```


```SQL
USE [test]
GO
/****** Object:  Table [dbo].[sampleTable]    Script Date: 06/08/2023 02:46:50 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[sampleTable](
	[id] [int] NOT NULL,
	[name] [nvarchar](50) NULL,
	[description] [nvarchar](max) NULL,
 CONSTRAINT [PK_sampleTable] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
INSERT [dbo].[sampleTable] ([id], [name], [description]) VALUES (1, N'Name', N'description')
GO
INSERT [dbo].[sampleTable] ([id], [name], [description]) VALUES (2, N'Name2', N'descriptopn2')
GO
INSERT [dbo].[sampleTable] ([id], [name], [description]) VALUES (3, N'Name3', N'description3')
GO
INSERT [dbo].[sampleTable] ([id], [name], [description]) VALUES (4, N'Name4', N'descirption 2')
GO
```


## Plugins

### Content Replacements
Purpose of this option

Gets applied to whole file at the a point of storing and comparing based on StoreInFile option. It requires a ResultsStoreOption to be set to Failure or All
StoreInFile option gets processed on saving file result.
All files gets processed when comparing the result.

 | Fields        | Explanation                                              |
 |:--            |:--                                                       |
 | From          | String we are looking for in file                        |
 | To            | To be replaced with another                              |
 | StoreInFile   | True / False                                             |

 Use Cases:

 1. I want to remove a domain specific part from the result so I can compare a data between environments and I know only paths are different.
 1. I want to remove authentication tokens.
 1. I want to validate a data set where I know a migration has changed in the api, so I can allow for migration to a new version by replacing substring and keep validating the same api.

#### Example of configuration

Add comparison option into `config.json` file.

```
    "ContentReplacements": [
        {
            "From": "htts://integration.christies.com",
            "To": "",
            "StoreInFile": false
        },
        {
            "From": "htts://staging.christies.com",
            "To": "",
            "StoreInFile": false
        },
        {
            "From": "authetication key",
            "To": "xxxxxx",
            "StoreInFile": true
        }
    ]
```