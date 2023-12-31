﻿using APITestingRunner.ApiRequest;
using APITestingRunner.Configuration;
using APITestingRunner.Database;
using APITestingRunner.Exceptions;
using APITestingRunner.IoOperations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Diagnostics;
using System.Reflection;

namespace APITestingRunner.Unit.Tests.DataTests
{
    [TestClass]
    public class DataAccessTests : TestBase
    {
        private readonly Config _config = new()
        {
            UrlBase = "http://localhost:5152/",
            CompareUrlBase = string.Empty,
            CompareUrlPath = string.Empty,
            UrlPath = "/Data",
            UrlParam =
                [
                    new Param("urlKey", "test"),
                    new Param("id", "sqlId")
                ],
            HeaderParam =
                [
                    new Param("accept", "application/json")
                ],
            RequestBody = null,
            DBConnectionString = DatabaseConnectionBuilder.BuildLocalhostDatabaseConnection(),
            DBQuery = "select id as sqlId from dbo.sampleTable;",
            DBFields =
                [
                    new Param("sqlId", "sqlId")
                ],
            RequestType = RequestType.GET,
            ResultsStoreOption = StoreResultsOption.All,
            ConfigMode = TesterConfigMode.Run,
            OutputLocation = DirectoryServices.AssemblyDirectory
        };

        //[TestInitialize]
        //public void TestInit()
        //{ }

        [TestMethod]
        public void DataAccess_Tests_ConstructorShouldPass()
        {
            _ = new DataAccess(_config, new Mock<ILogger>().Object);
        }

        //TODO: Review together - type Safety takes care of this - shouldn't be needed.
        [TestMethod]
        public void DataAccess_Tests_MissingConfig_ConstructorShouldThrowArgumentNullException()
        {
            _ = Assert.ThrowsException<ArgumentNullException>(() => new DataAccess(null!, null!));
        }

        //TODO: Review together - type Safety takes care of this - shouldn't be needed.
        [TestMethod]
        public void DataAccess_Tests_MissingLogger_ConstructorShouldThrowArgumentNullException()
        {
            _ = Assert.ThrowsException<ArgumentNullException>(() => new DataAccess(_config, null!));
        }

        [TestMethod]
        public async Task FetchDataForRunnerAsync_PassNullForConnectionString_shouldThrowConfigurationErrorsException()
        {
            var testConfig = _config;
            try
            {
                testConfig.DBConnectionString = null;
                DataAccess da = new(testConfig, new Mock<Logger>().Object);
                _ = await da.FetchDataForRunnerAsync();
                Assert.Fail();
            }
            catch (TestRunnerConfigurationErrorsException ex)
            {
                Assert.AreEqual("Failed to load connection string", ex.Message);
            }
            catch
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public async Task FetchDataForRunner_GetDataFromDatabase_ShouldReturn_DataSet_withOneFieldFromDbForBinder()
        {
            try
            {
                var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                _config.DBConnectionString = DatabaseConnectionBuilder.BuildFileDatabaseConnection(filePath, "SampleDb.mdf");

                var data = new DataAccess(_config, new Mock<Logger>().Object);

                var records = await data.FetchDataForRunnerAsync();

                _ = records.Should().NotBeEmpty();
                _ = records.Should().HaveCount(3);

                _ = records.Last().Should().BeEquivalentTo(new DataQueryResult
                {
                    RowId = 3,
                    Results = [new KeyValuePair<string, string>("sqlId", "3"),
                    ]
                });

                _ = records.Last().Results.Should().HaveCount(1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Shouldn't be here");
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public async Task FetchDataForRunner_GetDataFromDatabase_ShouldReturn_EmptyDataSet_withOneFieldFromDbForBinder()
        {
            try
            {
                var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
                _config.DBConnectionString = DatabaseConnectionBuilder.BuildFileDatabaseConnection(filePath, "SampleDb.mdf");

                _config.DBQuery = "select id as sqlId from dbo.sampleTable where id>5;";
                var data = new DataAccess(_config, new Mock<Logger>().Object);

                var records = await data.FetchDataForRunnerAsync();

                _ = records.Should().BeEmpty();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Shouldn't be here");
                Assert.Fail(ex.Message);
            }
        }


        [TestMethod]
        public async Task FetchDataForRunner_GetDataFromDatabase_ShouldReturn_SingleFieldDataSet_withOneFieldFromDbForBinder()
        {
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            _config.DBConnectionString = DatabaseConnectionBuilder.BuildFileDatabaseConnection(filePath, "SampleDb.mdf");

            _config.DBQuery = "select id as sqlId, name as fieldName from dbo.sampleTable";
            var data = new DataAccess(_config, new Mock<Logger>().Object);

            var records = await data.FetchDataForRunnerAsync();

            _ = records.Should().NotBeEmpty();
            _ = records.Should().HaveCount(3);

            _ = records.Last().Should().BeEquivalentTo(new DataQueryResult
            {
                RowId = 3,
                Results = [new KeyValuePair<string, string>("sqlId", "3"),
                ]
            });

            _ = records.Last().Results.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task FetchDataForRunner_GetDataFromDatabase_ShouldReturn_TwoFieldDataSet_withOneFieldFromDbForBinder()
        {
            var filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            _config.DBConnectionString = DatabaseConnectionBuilder.BuildFileDatabaseConnection(filePath, "SampleDb.mdf");

            _config.DBFields =
                [
                    new Param("sqlId", "sqlId"),
                    new Param("fieldName", "fieldName")
                ];

            _config.DBQuery = "select id as sqlId, name as fieldName from dbo.sampleTable";
            var data = new DataAccess(_config, new Mock<Logger>().Object);

            var records = await data.FetchDataForRunnerAsync();

            _ = records.Should().NotBeEmpty();
            _ = records.Should().HaveCount(3);

            _ = records.Last().RowId.Should().Be(3);
            _ = records.Last().Results.Should().HaveCount(2);
            _ = records.Last().Results.First().Should().BeEquivalentTo(new KeyValuePair<string, string>("sqlId", "3"));
            _ = records.Last().Results.Last().Should().BeEquivalentTo(new KeyValuePair<string, string>("fieldName", "Linux"), because: "");
        }
    }
}