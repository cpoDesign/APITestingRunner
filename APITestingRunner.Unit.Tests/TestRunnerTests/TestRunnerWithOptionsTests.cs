﻿using APITestingRunner.ApiRequest;
using APITestingRunner.Configuration;
using APITestingRunner.Database;
using APITestingRunner.IoOperations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace APITestingRunner.Unit.Tests
{
    [TestClass]
    public class TestRunnerWithOptionsWithConfigAPICallsTests
    {
        private WireMockServer? server;

        [TestInitialize]
        public void Initialize()
        {
            // This starts a new mock server instance listening at port 9876
            server = WireMockServer.Start(7055);
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
            _ = server ?? throw new ArgumentNullException(nameof(server));
            server.Stop();
        }

        [TestMethod]
        public void GenerateResultName_ShouldThrowExceptionIfPassedNull()
        {
            Action? act = () => TestRunner.GenerateResultName(null, null);
            _ = act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnConfig")]
        public async Task ValidateImplementationFor_SingleAPICallAsync_ShouldMakeAnAPICall_WithResult_200_noStoreOfFiles()
        {
            _ = server ?? throw new ArgumentNullException(nameof(server));

            server.Given(
                 WireMock.RequestBuilders.Request.Create().WithPath("/WeatherForecast").UsingGet()
           )
           .RespondWith(
               Response.Create()
                   .WithStatusCode(200)
                   .WithHeader("Content-Type", "text/plain")
                   .WithBody("Hello, world!")
           );

            Config config = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                UrlParam = null,
                RequestBody = null,
                HeaderParam = [
                                new Param("accept","application/json")
                              ],
                DBConnectionString = null,
                DBQuery = null,
                DBFields = null,
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.None,
                ConfigMode = TesterConfigMode.Run,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            var logger = new TestLogger();

            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(config);

            _ = testRunner.Errors.Should().BeEmpty();

            _ = logger.Messages.Should().ContainEquivalentOf(new Tuple<LogLevel, string>(LogLevel.Information, $"{config.RequestType} /WeatherForecast 200 success"));
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnConfig")]
        [TestCategory("StoreFiles")]
        public async Task ValidateImplementationFor_SingleAPICallAsync_ShouldMakeAnAPICall_WithResult_200_WithStoringFiles()
        {
            _ = server ?? throw new ArgumentNullException(nameof(server));

            server.Given(
                   WireMock.RequestBuilders.Request.Create().WithPath("/WeatherForecast").UsingGet()
               )
               .RespondWith(
                   Response.Create()
                       .WithStatusCode(200)
                       .WithHeader("Content-Type", "text/plain")
                       .WithBody("Hello, world!")
               );

            Config config = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                UrlParam = null,
                RequestBody = null,
                HeaderParam = [
                                new Param("accept","application/json")
                              ],
                DBConnectionString = null,
                DBQuery = null,
                DBFields = null,
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.All,
                ConfigMode = TesterConfigMode.Capture,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            var logger = new TestLogger();

            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(config);

            _ = testRunner.Errors.Should().BeEmpty();

            _ = logger.Messages.Should().ContainEquivalentOf(new Tuple<LogLevel, string>(LogLevel.Information, $"{config.RequestType} /WeatherForecast 200 success Results/request-0.json"));


            var expectedFilePath = DirectoryServices.AssemblyDirectory;

            var testDirectory = Path.Combine(expectedFilePath, TestConstants.TestOutputDirectory);
            _ = Directory.Exists(testDirectory).Should().BeTrue(because: $"directory is: {testDirectory}");

            var fileName = Path.Combine(testDirectory, TestRunner.GenerateResultName(new DataQueryResult() { RowId = 0 }));

            _ = File.Exists(fileName).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnConfig")]
        [TestCategory("StoreFiles")]
        public async Task CreateConfigForSingleAPICall_ShouldStoreFile_whenErrorIsReceived_WithResult500()
        {
            _ = server ?? throw new ArgumentNullException(nameof(server));

            server.Given(
                  WireMock.RequestBuilders.Request.Create().WithPath("/WeatherForecast").UsingGet()
              )
              .RespondWith(
                  Response.Create()
                      .WithStatusCode(500)
                      .WithHeader("Content-Type", "text/plain")
                      .WithBody("Exception on the server")
              );

            Config config = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                UrlParam = null,
                RequestBody = null,
                HeaderParam = [
                                new Param("accept","application/json")
                              ],
                DBConnectionString = null,
                DBQuery = null,
                DBFields = null,
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.FailuresOnly,
                ConfigMode = TesterConfigMode.Capture,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            var logger = new TestLogger();

            // Act
            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(config);


            // assert
            _ = testRunner.Errors.Should().HaveCount(0);
            _ = logger.Messages.Should().ContainEquivalentOf(new Tuple<LogLevel, string>(LogLevel.Information, $"{config.RequestType} /WeatherForecast 500 fail Results/request-0.json"));


            var expectedFilePath = DirectoryServices.AssemblyDirectory;

            var testDirectory = Path.Combine(expectedFilePath, TestConstants.TestOutputDirectory);
            _ = Directory.Exists(testDirectory).Should().BeTrue(because: $"directory is: {testDirectory}");

            var fileName = Path.Combine(testDirectory, TestRunner.GenerateResultName(new DataQueryResult() { RowId = 0 }));

            _ = File.Exists(fileName).Should().BeTrue();
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnConfig")]
        [TestCategory("StoreFiles")]
        public async Task CreateConfigForSingleAPICall_ShouldStoreFile_whenErrorIsReceived_WithResult200_ShouldNotStoreResultFile()
        {
            _ = server ?? throw new ArgumentNullException(nameof(server));

            server.Given(
                WireMock.RequestBuilders.Request.
                    Create()
                    .WithPath("/WeatherForecast")
                    .UsingGet()
              )
              .RespondWith(
                  Response.Create()
                      .WithStatusCode(200)
                      .WithHeader("Content-Type", "text/plain")
                      .WithBody("Exception on the server")
              );

            Config config = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                UrlParam = null,
                RequestBody = null,
                HeaderParam = [
                                new Param("accept","application/json")
                              ],
                DBConnectionString = null,
                DBQuery = null,
                DBFields = null,
                RequestType = RequestType.GET,
                ResultsStoreOption = StoreResultsOption.FailuresOnly,
                ConfigMode = TesterConfigMode.Capture,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            var logger = new TestLogger();

            // Act
            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(config);


            // assert
            _ = testRunner.Errors.Should().HaveCount(0);
            _ = logger.Messages.Should().ContainEquivalentOf(new Tuple<LogLevel, string>(LogLevel.Information, $"{config.RequestType} /WeatherForecast 200 success"));

            var expectedFilePath = DirectoryServices.AssemblyDirectory;
            var testDirectory = Path.Combine(expectedFilePath, TestConstants.TestOutputDirectory);
            var fileName = Path.Combine(testDirectory, TestRunner.GenerateResultName(new DataQueryResult() { RowId = 0 }));

            _ = Directory.Exists(testDirectory).Should().BeFalse(because: $"directory is: {testDirectory}");


            _ = File.Exists(fileName).Should().BeFalse();
        }

        [TestMethod]
        [TestCategory("SimpleAPICallBasedOnConfig")]
        [DataRow(200, "success", StoreResultsOption.None, false)]
        [DataRow(500, "fail", StoreResultsOption.None, false)]
        [DataRow(200, "success", StoreResultsOption.FailuresOnly, false)]
        [DataRow(500, "fail", StoreResultsOption.FailuresOnly, true)]
        [DataRow(200, "success", StoreResultsOption.All, true)]
        [DataRow(500, "fail", StoreResultsOption.All, true)]
        public async Task CreateConfigForSingleAPICall_ShouldValidateStoreOptionsBaseOnAPiResult(int statusCode, string determination, StoreResultsOption storeOption, bool directoryAndFileExists)
        {
            ArgumentNullException.ThrowIfNull(nameof(server));

            server!.Given(WireMock.RequestBuilders.Request.Create().WithPath("/WeatherForecast").UsingGet())
              .RespondWith(
                  Response.Create()
                      .WithStatusCode(statusCode)
                      .WithHeader("Content-Type", "text/plain")
                      .WithBody("Exception on the server")
              );

            Config config = new()
            {
                UrlBase = "http://localhost:7055",
                CompareUrlBase = string.Empty,
                CompareUrlPath = string.Empty,
                UrlPath = "/WeatherForecast",
                UrlParam = null,
                RequestBody = null,
                HeaderParam = [
                                new Param("accept","application/json")
                              ],
                DBConnectionString = null,
                DBQuery = null,
                DBFields = null,
                RequestType = RequestType.GET,
                ResultsStoreOption = storeOption,
                ConfigMode = TesterConfigMode.Capture,
                OutputLocation = DirectoryServices.AssemblyDirectory,
            };

            var logger = new TestLogger();

            // Act
            var testRunner = await new ApiTesterRunner(logger)
                                .RunTests(config);

            // assert
            _ = testRunner.Errors.Should().HaveCount(0);

            var fileNameLog = string.Empty;
            if (directoryAndFileExists)
            {
                fileNameLog += $" Results/request-0.json";
            }
            _ = logger.Messages.Should().ContainEquivalentOf(new Tuple<LogLevel, string>(LogLevel.Information, $"{config.RequestType} /WeatherForecast {statusCode} {determination}{fileNameLog}"));

            var expectedFilePath = DirectoryServices.AssemblyDirectory;
            var testDirectory = Path.Combine(expectedFilePath, TestConstants.TestOutputDirectory);
            var fileName = Path.Combine(testDirectory, TestRunner.GenerateResultName(new DataQueryResult() { RowId = 0 }));

            if (directoryAndFileExists)
            {
                _ = Directory.Exists(testDirectory).Should().BeTrue(because: $"directory is: {testDirectory}");
                _ = File.Exists(fileName).Should().BeTrue();
            }
            else
            {
                _ = Directory.Exists(testDirectory).Should().BeFalse(because: $"directory is: {testDirectory}");
                _ = File.Exists(fileName).Should().BeFalse();
            }
        }
    }
}