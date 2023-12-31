﻿// See https://aka.ms/new-console-template for more information

//Console.WriteLine("Hello, World!");


//Console.WriteLine(path);
//Console.ReadLine();

using APITestingRunner.Configuration;
using APITestingRunner.Plugins;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace APITestingRunner
{
    public class ApiTesterRunner(ILogger logger)
    {
        private readonly ILogger? _logger = logger ?? throw new ArgumentNullException(nameof(logger));


        private readonly IList<IPlugin> PluginList = new List<IPlugin>(){
                                new ContentReplacements()
                             };


        public async Task CreateConfig(string pathConfigJson, Config config)
        {
            _ = _logger ?? throw new ArgumentNullException(nameof(_logger));

            _logger.LogInformation($"creating config on path: {pathConfigJson}");
            await ConfigurationManager
                 .CreateConfig(pathConfigJson, config);
            _logger.LogInformation($"creating created");
            return;

        }
        public async Task RunTests(string pathConfigJson)
        {
            Console.WriteLine($"Loading config on path: {pathConfigJson}");

            _ = _logger ?? throw new ArgumentNullException(nameof(_logger));

            var configSettings = await ConfigurationManager.GetConfigAsync(pathConfigJson);
            var testRunner = await this.RunTestWithPlugins(configSettings);

            _ = await testRunner.PrintResultsSummary();
            return;
        }

        public async Task<TestRunner> RunTests(Config config)
        {
            _ = _logger ?? throw new ArgumentNullException("_logger");

            return await RunTestWithPlugins(config);
        }

        private async Task<TestRunner> RunTestWithPlugins(Config config)
        {
            TestRunner testRunner = new(_logger);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                stopwatch.Start();

                testRunner.ApplyConfig(config);
                testRunner.RegisterPlugin(this.PluginList);
                _ = await testRunner.RunTestsAsync();
                stopwatch.Stop();
                _logger.LogInformation($"Total process took: {stopwatch.Elapsed:mm\\:ss\\.ff}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error caught: {ex.Message}", ex);
            }

            return testRunner;
        }
    }
}