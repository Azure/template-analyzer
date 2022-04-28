// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Templates.Analyzer.Cli;

namespace Analyzer.Cli.FunctionalTests
{
    [TestClass]
    public class CommandLineParserTests
    {
        private CommandLineParser _commandLineParser;

        [TestInitialize]
        public void TestInit()
        {
            _commandLineParser = new CommandLineParser();
        }

        [DataTestMethod]
        [DataRow("Path does not exist", 2, DisplayName = "Invalid template file path provided")]
        [DataRow("Configuration.json", 4, DisplayName = "Path exists, not an ARM template.")]
        [DataRow("Configuration.json", 3, "--report-format", "Sarif", DisplayName = "Path exists, Report-format flag set, --output-file-path flag not included.")]
        [DataRow("Configuration.json", 1, "--parameters-file-path", DisplayName = "Path exists, Parameters-file-path flag included, but no value provided.")]
        [DataRow("AppServicesLogs-Failures.json", 5, DisplayName = "Violations found in the template")]
        [DataRow("AppServicesLogs-Passes.json", 0, DisplayName = "Success")]
        public void AnalyzeTemplate_ValidInputValues_ReturnExpectedExitCode(string relativeTemplatePath, int expectedExitCode, params string[] additionalCliOptions)
        {
            var args = new string[] { "analyze-template" , Path.Combine(Directory.GetCurrentDirectory(), relativeTemplatePath)}; 
            args = args.Concat(additionalCliOptions).ToArray();
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(expectedExitCode, result.Result);
        }

        [DataTestMethod]
        [DataRow("Configuration.json", 1, DisplayName = "Provided parameters file is not a parameters file")]
        [DataRow("Parameters.json", 5, DisplayName = "Provided parameters file correct, issues in template")]
        public void AnalyzeTemplate_ParameterFileParamUsed_ReturnExpectedExitCode(string relativeParametersFilePath, int expectedExitCode)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "AppServicesLogs-Failures.json");
            var parametersFilePath = Path.Combine(Directory.GetCurrentDirectory(), relativeParametersFilePath);
            var args = new string[] { "analyze-template", templatePath, "--parameters-file-path", parametersFilePath };
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(expectedExitCode, result.Result);
        }

        [TestMethod]
        public void AnalyzeTemplate_UseConfigurationFileOption_ReturnExpectedExitCodeUsingOption()
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "AppServicesLogs-Failures.json");
            var configurationPath = Path.Combine(Directory.GetCurrentDirectory(), "Configuration.json");
            var args = new string[] { "analyze-template", templatePath, "--config-file-path", configurationPath};
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(5, result.Result);
        }

        [TestMethod]
        public void AnalyzeTemplate_ReportFormatAsSarif_ReturnExpectedExitCodeUsingOption()
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "AppServicesLogs-Failures.json");
            var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "outPutFile.sarif");
            // The FileStream has to be closed for the cli to be able to write values into it
            using (var fs = File.Create(outputFilePath))
            { }
            var args = new string[] { "analyze-template", templatePath, "--report-format", "Sarif", "--output-file-path", outputFilePath };
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(5, result.Result);
            
            File.Delete(outputFilePath);
        }

        [DataTestMethod]
        [DataRow(false, 2, DisplayName = "Invalid directory path provided")]
        [DataRow(true, 3, "--report-format", "Sarif", DisplayName = "Directory exists, Report-format flag set, --output-file-path flag not included.")]
        [DataRow(true, 1, "--report-format", "Console", "--output-file-path", DisplayName = "Path exists, Report-format flag set, --output-file-path flag included, but no value provided.")]
        [DataRow(true, 6, DisplayName = "Error + Violation: Scan has both errors and violations")]
        public void AnalyzeDirectory_ValidInputValues_ReturnExpectedExitCode(bool useTestDirectoryPath, int expectedExitCode, params string[] additionalCliOptions)
        {
            var args = new string[] { "analyze-directory", useTestDirectoryPath ? Directory.GetCurrentDirectory() : "Directory does not exist" };

            args = args.Concat(additionalCliOptions).ToArray();
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(expectedExitCode, result.Result);
        }
    }
}
