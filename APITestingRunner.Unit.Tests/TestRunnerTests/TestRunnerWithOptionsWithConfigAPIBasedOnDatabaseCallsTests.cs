﻿using APITestingRunner.ApiRequest;
using APITestingRunner.Configuration;
using APITestingRunner.Database;
using APITestingRunner.IoOperations;
using FluentAssertions;
using WireMock.Matchers;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace APITestingRunner.Unit.Tests.TestRunnerTests
{
    [TestClass]
    public class TestRunnerWithOptionsWithConfigAPIBasedOnDatabaseCallsTests : TestBase
    {
        private WireMockServer server = null!;

        [TestInitialize]
        public new void Initialize()
        {
            base.Initialize();

            // This starts a new mock server instance listening at port 9876
            server = WireMockServer.Start(7055);

            base.Initialize();
        }

        [TestCleanup]
        public void Cleanup()
        {
            var expectedFilePath = DirectoryServices.AssemblyDirectory;

            var testDirectory = Path.Combine(expectedFilePath, TestConstants.TestOutputDirectory);
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }

            // This stops the mock server to clean up after ourselves
            server.Stop();
        }

        [TestMethod]
        public void GenerateResultName_ShouldThrowExceptionIfPassedNull()
        {
            Action act = () => TestRunner.GenerateResultName(null);
            _ = act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnDbSource")]
        [TestCategory("dbcapture")]
        public async Task ValidateImplementationFor_SingleAPICallAsync_ShouldMakeAnAPICall_WithResult_200_noStoreOfFiles()
        {

            server.Given(
               WireMock.RequestBuilders.Request.Create()
               .WithPath("/WeatherForecast")
               .WithParam("urlKey", "configKey")
               .WithParam("id", new WildcardMatcher(MatchBehaviour.AcceptOnMatch, "*", true))
               .UsingGet()
             )
             .RespondWith(
                 Response.Create()
                     .WithStatusCode(200)
                     .WithHeader("Content-Type", "text/plain")
                     .WithBody("Hello, world!")
             );

            Config apiTesterConfig = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                RequestBody = null,
                HeaderParam = [
                    new Param("accept","application/json")
                ],
                UrlParam = [
                    new Param("urlKey", "configKey"),
                    new Param("id", "bindingId")
                ],
                DBConnectionString = _dbConnectionStringForTests,
                DBQuery = "select id as bindingId from dbo.sampleTable;",
                DBFields = [
                    new Param("bindingId", "bindingId"),
                ],
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.None,
                ConfigMode = TesterConfigMode.Run,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            TestLogger logger = new();

            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(apiTesterConfig);

            _ = testRunner.Errors.Should().BeEmpty();
            _ = logger.Messages.Count.Should().Be(7);

            _ = logger.Messages[0].Item2.Should().ContainEquivalentOf("Validating database based data source start");
            _ = logger.Messages[1].Item2.Should().ContainEquivalentOf("Found database connection string");
            _ = logger.Messages[2].Item2.Should().ContainEquivalentOf("Found database query and db fields. Attempting to load data from database.");
            _ = logger.Messages[3].Item2.Should().ContainEquivalentOf("/WeatherForecast?urlkey=configKey&id=1 200 success");
            _ = logger.Messages[4].Item2.Should().ContainEquivalentOf("/WeatherForecast?urlkey=configKey&id=2 200 success");
            _ = logger.Messages[5].Item2.Should().ContainEquivalentOf("/WeatherForecast?urlkey=configKey&id=3 200 success");
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnDbSourceWithCapture")]
        [TestCategory("dbcapture")]
        public async Task ValidateImplementationFor_SingleAPICallAsync_ShouldMakeAnAPICall_WithResult_404_WithFailureCapture()
        {
            server.Given(
               WireMock.RequestBuilders.Request.Create()
               .WithPath("/WeatherForecast")
               .WithParam("urlKey", "configKey")
               .WithParam("id", new WildcardMatcher(MatchBehaviour.AcceptOnMatch, "*", true))
               .UsingGet()
             )
             .RespondWith(
                 Response.Create()
                    .WithStatusCode(404)
                    .WithHeader("Content-Type", "text/plain")
                    .WithBody("Hello, world!")

             );

            Config apiTesterConfig = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                RequestBody = null,
                HeaderParam = [
                    new Param("accept","application/json")
                ],
                UrlParam = [
                    new Param("urlKey", "configKey"),
                    new Param("id", "bindingId")
                ],
                DBConnectionString = _dbConnectionStringForTests,
                DBQuery = "select id as bindingId from dbo.sampleTable;",
                DBFields = [
                    new Param("bindingId", "bindingId"),
                ],
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.FailuresOnly,
                ConfigMode = TesterConfigMode.Capture,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            TestLogger logger = new();

            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(apiTesterConfig);

            _ = testRunner.Errors.Should().BeEmpty();
            _ = logger.Messages.Count.Should().Be(7);

            _ = logger.Messages[0].Item2.Should().ContainEquivalentOf("Validating database based data source start");
            _ = logger.Messages[1].Item2.Should().ContainEquivalentOf("Found database connection string");
            _ = logger.Messages[2].Item2.Should().ContainEquivalentOf("Found database query and db fields. Attempting to load data from database.");
            _ = logger.Messages[3].Item2.Should().ContainEquivalentOf("/WeatherForecast?urlkey=configKey&id=1 404 fail Results/request-1.json");
            _ = logger.Messages[4].Item2.Should().ContainEquivalentOf("/WeatherForecast?urlkey=configKey&id=2 404 fail Results/request-2.json");
            _ = logger.Messages[5].Item2.Should().ContainEquivalentOf("/WeatherForecast?urlkey=configKey&id=3 404 fail Results/request-3.json");

            var expectedFilePath = DirectoryServices.AssemblyDirectory;

            var testDirectory = Path.Combine(expectedFilePath, TestConstants.TestOutputDirectory);
            _ = Directory.Exists(testDirectory).Should().BeTrue(because: $"directory is: {testDirectory}");

            var fileName = Path.Combine(testDirectory, TestRunner.GenerateResultName(new DataQueryResult() { RowId = 1 }));
            var fileName2 = Path.Combine(testDirectory, TestRunner.GenerateResultName(new DataQueryResult() { RowId = 2 }));
            var fileName3 = Path.Combine(testDirectory, TestRunner.GenerateResultName(new DataQueryResult() { RowId = 3 }));

            _ = File.Exists(fileName).Should().BeTrue();
            _ = File.Exists(fileName2).Should().BeTrue();
            _ = File.Exists(fileName3).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnDbSourceWithCapture")]
        [TestCategory("ResultCompare")]
        public async Task ValidateImplementationFor_SingleAPICallAsync_ShouldMakeAnAPICall_WithResult_200_ShouldStoreAllRequest_withFileNamingBasedOnDbResult()
        {
            server.Given(
               WireMock.RequestBuilders.Request.Create()
               .WithPath("/WeatherForecast")
               .WithParam("urlKey", "configKey")
               .WithParam("id", new WildcardMatcher(MatchBehaviour.AcceptOnMatch, "*", true))
               .UsingGet()
             )
             .RespondWith(
                 Response.Create()
                     .WithStatusCode(200)
                     .WithHeader("Content-Type", "application/json")
                     .WithBody("{\"name\":\"John\",\"age\":30,\"city\":\"New York\",\"hobbies\":[{\"type\":\"reading\",\"locations\":[{\"name\":\"Library\",\"hours\":9},{\"name\":\"Park\",\"hours\":5}]}]}")
             );

            Config apiTesterConfig = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                RequestBody = null,
                HeaderParam = [
                    new Param("accept", "application/json")
                ],
                UrlParam = [
                    new Param("urlKey", "configKey"),
                    new Param("id", "bindingId")
                ],
                DBConnectionString = _dbConnectionStringForTests,
                DBQuery = "select id as bindingId, RecordType as fileRecordType from dbo.sampleTable where id in (1,3)",
                DBFields = [
                    new Param("bindingId", "bindingId"),
                    new Param("fileRecordType", "fileRecordType"),
                ],
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.All,
                ResultFileNamePattern = "{fileRecordType}-{bindingId}",
                ConfigMode = TesterConfigMode.CaptureAndCompare,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            TestLogger logger = new();

            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(apiTesterConfig);

            _ = testRunner.Errors.Should().BeEmpty();
            _ = logger.Messages.Count.Should().Be(6);

            _ = logger.Messages[0].Item2.Should().ContainEquivalentOf("Validating database based data source start");
            _ = logger.Messages[1].Item2.Should().ContainEquivalentOf("Found database connection string");
            _ = logger.Messages[2].Item2.Should().ContainEquivalentOf("Found database query and db fields. Attempting to load data from database.");
            _ = logger.Messages[3].Item2.Should().ContainEquivalentOf("/WeatherForecast?urlkey=configKey&id=1 200 success Results/request-music-1.json NewFile");
            _ = logger.Messages[4].Item2.Should().ContainEquivalentOf("/WeatherForecast?urlkey=configKey&id=3 200 success Results/request-software-3.json NewFile");
            _ = logger.Messages[5].Item2.Should().Contain("Total process took:");

            // file validation
            var expectedFilePath = DirectoryServices.AssemblyDirectory;
            var testDirectory = Path.Combine(expectedFilePath, TestConstants.TestOutputDirectory);
            _ = Directory.Exists(testDirectory).Should().BeTrue(because: $"directory is: {testDirectory}");

            var fileName = Path.Combine(testDirectory, "request-music-1.json");
            var fileName3 = Path.Combine(testDirectory, "request-software-3.json");

            _ = File.Exists(fileName).Should().BeTrue(because: $"Expected file name {fileName}");
            _ = File.Exists(fileName3).Should().BeTrue(because: $"Expected file name {fileName3}");

            // now lets rerun and see the differences
            // clear logger
            logger = new TestLogger();

            apiTesterConfig.DBQuery = "select id as bindingId, RecordType as fileRecordType from dbo.sampleTable";

            testRunner = await new ApiTesterRunner(logger)
                                .RunTests(apiTesterConfig);

            _ = testRunner.Errors.Should().BeEmpty();
            var loggerMessages = logger.Messages.Select(x => x.Item2);
            _ = loggerMessages.Should().HaveCount(17);

            _ = loggerMessages.Should().ContainEquivalentOf("Validating database based data source start");
            _ = loggerMessages.Should().ContainEquivalentOf("Found database connection string");
            _ = loggerMessages.Should().ContainEquivalentOf("Found database query and db fields. Attempting to load data from database.");
            _ = loggerMessages.Should().ContainEquivalentOf("GET /WeatherForecast?urlKey=configKey&id=1 200 success Results/request-music-1.json Matching");

            _ = loggerMessages.Should().ContainEquivalentOf("GET /WeatherForecast?urlKey=configKey&id=2 200 success Results/request-software-2.json NewFile");
            _ = loggerMessages.Should().ContainEquivalentOf("GET /WeatherForecast?urlKey=configKey&id=3 200 success Results/request-software-3.json Matching");

            _ = Path.Combine(testDirectory, "request-music-1.json");
            _ = Path.Combine(testDirectory, "request-software-2.json");
            _ = Path.Combine(testDirectory, "request-software-3.json");
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnDbSource")]
        [TestCategory("dbcapture")]
        public async Task ValidateImplementationFor_SingleAPICallAsync_ShouldAppendIdToUrl()
        {
            server.Given(
               WireMock.RequestBuilders.Request.Create()
               .WithPath("/WeatherForecast/1")

               .UsingGet()
             )
             .RespondWith(
                 Response.Create()
                     .WithStatusCode(200)
                     .WithHeader("Content-Type", "text/plain")
                     .WithBody("Hello, world!")
             );

            Config apiTesterConfig = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast/{bindingId}",
                RequestBody = null,
                HeaderParam = [
                    new Param("accept","application/json")
                ],
                UrlParam = null!,
                DBConnectionString = _dbConnectionStringForTests,
                DBQuery = "select top 1 id as bindingId from dbo.sampleTable;",
                DBFields = [
                    new Param("bindingId", "bindingId"),
                ],
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.None,
                ConfigMode = TesterConfigMode.Run,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            TestLogger logger = new();

            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(apiTesterConfig);

            _ = testRunner.Errors.Should().BeEmpty();
            _ = logger.Messages.Count.Should().Be(5);

            _ = logger.Messages[0].Item2.Should().ContainEquivalentOf("Validating database based data source start");
            _ = logger.Messages[1].Item2.Should().ContainEquivalentOf("Found database connection string");
            _ = logger.Messages[2].Item2.Should().ContainEquivalentOf("Found database query and db fields. Attempting to load data from database.");
            _ = logger.Messages[3].Item2.Should().ContainEquivalentOf("/WeatherForecast/1 200 success");
            _ = logger.Messages[4].Item2.Should().Contain("Total process took:");
        }
    }
}