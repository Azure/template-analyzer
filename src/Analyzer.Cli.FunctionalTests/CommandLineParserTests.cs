// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Templates.Analyzer.Cli;
using Newtonsoft.Json.Linq;

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
            var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "OutputFile.sarif");
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

        [TestMethod]
        public void AnalyzeDirectory_DirectoryWithOtherJsonFiles_LogsExpectedErrorInSarif()
        {
            var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Output.sarif");
            var directoryToAnalyze = Path.Combine(Directory.GetCurrentDirectory(), "ADirectoryToAnalyze");
            
            var args = new string[] { "analyze-directory", directoryToAnalyze, "--report-format", "Sarif", "--output-file-path", outputFilePath };

            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(1, result.Result);

            var sarifOutput = JObject.Parse(File.ReadAllText(outputFilePath));
            var toolNotifications = sarifOutput["runs"][0]["invocations"][0]["toolExecutionNotifications"];

            var templateErrorMessage = "An exception occurred while analyzing a template";
            Assert.AreEqual(templateErrorMessage, toolNotifications[0]["message"]["text"]);
            Assert.AreEqual(templateErrorMessage, toolNotifications[1]["message"]["text"]);

            Assert.AreEqual($"Unable to analyze 2 file(s): {Path.Combine(directoryToAnalyze, "ANonTemplateJsonFile.json")}, {Path.Combine(directoryToAnalyze, "AnotherNonTemplateJsonFile.json")}", toolNotifications[2]["message"]["text"]);
            
            Assert.AreEqual("error", toolNotifications[0]["level"]);
            Assert.AreEqual("error", toolNotifications[1]["level"]);
            Assert.AreEqual("error", toolNotifications[2]["level"]);

            Assert.AreNotEqual(null, toolNotifications[0]["exception"]);
            Assert.AreNotEqual(null, toolNotifications[1]["exception"]);
            Assert.AreEqual(null, toolNotifications[2]["exception"]);
        }
    }
}