﻿using FluentAssertions;
using Microsoft.Extensions.Logging;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static ConfigurationManager;

namespace APITestingRunner.Unit.Tests {
    [TestClass]
    public partial class TestRunnerWithOptionsWithConfigAPIBasedOnDatabaseCallsTests {

        private WireMockServer server;

        [TestInitialize]
        public void Initialize() {
            // This starts a new mock server instance listening at port 9876
            server = WireMockServer.Start(7055);
        }

        [TestCleanup]
        public void Cleanup() {
            var expectedFilePath = DirectoryServices.AssemblyDirectory;

            var testDirectory = Path.Combine(expectedFilePath, TestConstants.TestOutputDirectory);
            if (Directory.Exists(testDirectory)) {
                Directory.Delete(testDirectory, true);
            }

            // delete all file in results directory
            // This stops the mock server to clean up after ourselves
            server.Stop();
        }

        [TestMethod]
        public void GenerateResultName_ShouldThrowExceptionIfPassedNull() {
            Action act = () => TestRunner.GenerateResultName(null);
            _ = act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnDbSource")]
        public async Task ValidateImplementationFor_SingleAPICallAsync_ShouldMakeAnAPICall_WithResult_200_noStoreOfFiles() {
            server.Given(
                 WireMock.RequestBuilders.Request
                .Create()
                .WithParam("urlKey", "key")
                .WithParam("id", new string[] { "1", "2", "3" })
                .WithPath("/WeatherForecast").UsingGet()
           )
           .RespondWith(
               Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "text/plain")
                   .WithBody("Hello, world!")
           );

            Config config = new() {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                RequestBody = null,
                HeaderParam = new List<Param> {
                                new Param("accept","application/json")
                              },
                UrlParam = new List<Param>
                  {
                    new Param("urlKey", "test"),
                    new Param("id", "sqlId")
                  },
                DBConnectionString = null,
                DBQuery = "select id as sqlId from dbo.sampleTable;",
                DBFields = new List<Param>
                  {
                    new Param("sqlId", "sqlId")
                  },
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.None,
                ConfigMode = TesterConfigMode.Run,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            config.DBConnectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\code\\cpoDesign\\APITestingRunner\\APITestingRunner.Unit.Tests\\SampleDb.mdf;Integrated Security=True";

            config.DBFields = new List<Param>
              {
                new Param("sqlId", "sqlId"),
                new Param("fieldName", "fieldName")
              };

            var logger = new TestLogger();

            var testRunner = await new IndividualActions()
                                .AddLogger(logger)
                                .RunTests(config);

            _ = testRunner.Errors.Should().BeEmpty();

            _ = logger.Messages.Should().ContainEquivalentOf(new Tuple<LogLevel, string>(LogLevel.Information, "/WeatherForecast 200 success"));
        }


        //[TestMethod]
        //public async Task CreateConfigForSingleAPICallWithUrlParam()
        //{
        //    _ = new Config()
        //    {
        //        UrlBase = "https://localhost:7055",
        //        CompareUrlBase = string.Empty,
        //        CompareUrlPath = string.Empty,
        //        UrlPath = "/WeatherForecast/GetWeatherForecastForLocation",
        //        UrlParam = new List<Param>
        //  {
        //    new Param("location","UK")
        //  },
        //        HeaderParam = new List<Param> {
        //  new Param("accept","application/json")
        //},
        //        RequestBody = null,
        //        DBConnectionString = null,
        //        DBQuery = null,
        //        DBFields = null,
        //        RequestType = RequestType.GET,
        //        ResultsStoreOption = StoreResultsOption.None,
        //        ConfigMode = TesterConfigMode.Run,
        //        LogLocation = DirectoryServices.AssemblyDirectory
        //    };
        //    Assert.Fail();
        //}

        ////[DataRow(StoreResultsOption.None)]
        ////[DataRow(StoreResultsOption.FailuresOnly)]
        ////[DataRow(StoreResultsOption.All)]
        ////public async Task CreateConfigForDatabaseBasedAPICall(StoreResultsOption storeResultsOption)
        ////{
        //[TestMethod]
        //public async Task CreateConfigForDatabaseBasedAPICall()
        //{
        //    StoreResultsOption storeResultsOption = StoreResultsOption.All;

        //    string sqlCon = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\code\cpoDesign\APITestingRunner\APITestingRunner.Unit.Tests\SampleDb.mdf;Integrated Security=True";


        //    Config config = new()
        //    {
        //        UrlBase = "https://localhost:7055",
        //        CompareUrlBase = string.Empty,
        //        CompareUrlPath = string.Empty,
        //        UrlPath = "/Data",
        //        UrlParam = new List<Param>
        //{
        //  new Param("urlKey", "test"),
        //  new Param("id", "sqlId")
        //},
        //        HeaderParam = new List<Param> {
        //  new Param("accept","application/json")
        //},
        //        RequestBody = null,
        //        DBConnectionString = sqlCon,
        //        DBQuery = "select id as sqlId from dbo.sampleTable;",
        //        DBFields = new List<Param>
        //{
        //  new Param("sqlId", "sqlId")
        //},
        //        RequestType = RequestType.GET,
        //        ResultsStoreOption = storeResultsOption,
        //        ConfigMode = TesterConfigMode.Run,
        //        LogLocation = DirectoryServices.AssemblyDirectory
        //    };


        //    _ = await IndividualActions.RunTests(config);
        //}

        //[TestMethod]
        //public async Task CreateConfigForDatabaseBasedAPIComparrisonCall()
        //{
        //    Config config = new()
        //    {
        //        UrlBase = "https://localhost:7055",
        //        CompareUrlBase = "https://localhost:7055",
        //        UrlPath = "/Data",
        //        CompareUrlPath = "/DataV2",
        //        UrlParam = new List<Param>
        //{
        //  new Param("urlKey", "test"),
        //  new Param("id", "sqlId")
        //},
        //        HeaderParam = new List<Param> {
        //  new Param("accept","application/json")
        //},
        //        RequestBody = null,
        //        DBConnectionString = "Server=127.0.0.1; Database=test; User Id=sa; Password=<YourStrong@Passw0rd>;TrustServerCertificate=True;",
        //        DBQuery = "select id as sqlId from dbo.sampleTable;",
        //        DBFields = new List<Param>
        //{
        //  new Param("sqlId", "sqlId")
        //},
        //        RequestType = RequestType.GET,
        //        ResultsStoreOption = StoreResultsOption.None,
        //        ConfigMode = TesterConfigMode.APICompare,
        //        LogLocation = DirectoryServices.AssemblyDirectory
        //    };


        //    await IndividualActions.RunTests(config);
        //}

        //[TestMethod]
        //public async Task CreateConfigForSingleAPICallWithUrlParamAndBodyModel()
        //{

        //    Config config = new()
        //    {
        //        UrlBase = "https://localhost:7055",
        //        CompareUrlBase = string.Empty,
        //        CompareUrlPath = string.Empty,
        //        UrlPath = "/datamodel/123456789",
        //        UrlParam = new List<Param>
        //  {
        //    new Param("location","UK")
        //  },
        //        HeaderParam = new List<Param> {
        //  new Param("accept","application/json")
        //},
        //        RequestBody = "{Id={sqlId},StaticData=\"data\"}",
        //        DBConnectionString = null,
        //        DBQuery = null,
        //        DBFields = null,
        //        RequestType = RequestType.GET,
        //        ResultsStoreOption = StoreResultsOption.None,
        //        ConfigMode = TesterConfigMode.Run,
        //        LogLocation = DirectoryServices.AssemblyDirectory
        //    };

        //    await IndividualActions.RunTests(config);
        //}
    }
}