// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
            var args = new string[] { "analyze-template" , GetFilePath(relativeTemplatePath)}; 
            args = args.Concat(additionalCliOptions).ToArray();
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(expectedExitCode, result.Result);
        }

        [DataTestMethod]
        [DataRow("Configuration.json", 1, DisplayName = "Provided parameters file is not a parameters file")]
        [DataRow("Parameters.json", 5, DisplayName = "Provided parameters file correct, issues in template")]
        public void AnalyzeTemplate_ParameterFileParamUsed_ReturnExpectedExitCode(string relativeParametersFilePath, int expectedExitCode)
        {
            var templatePath = GetFilePath("AppServicesLogs-Failures.json");
            var parametersFilePath = GetFilePath(relativeParametersFilePath);
            var args = new string[] { "analyze-template", templatePath, "--parameters-file-path", parametersFilePath };
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(expectedExitCode, result.Result);
        }

        [TestMethod]
        public void AnalyzeTemplate_UseConfigurationFileOption_ReturnExpectedExitCodeUsingOption()
        {
            var templatePath = GetFilePath("AppServicesLogs-Failures.json");
            var configurationPath = GetFilePath("Configuration.json");
            var args = new string[] { "analyze-template", templatePath, "--config-file-path", configurationPath};
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(5, result.Result);
        }

        [TestMethod]
        public void AnalyzeTemplate_ReportFormatAsSarif_ReturnExpectedExitCodeUsingOption()
        {
            var templatePath = GetFilePath("AppServicesLogs-Failures.json");
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
        public void AnalyzeDirectory_DirectoryWithInvalidTemplates_LogsExpectedErrorInSarif()
        {
            var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Output.sarif");
            var directoryToAnalyze = GetFilePath("ToTestSarifNotifications");
            
            var args = new string[] { "analyze-directory", directoryToAnalyze, "--report-format", "Sarif", "--output-file-path", outputFilePath };

            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(1, result.Result);

            var sarifOutput = JObject.Parse(File.ReadAllText(outputFilePath));
            var toolNotifications = sarifOutput["runs"][0]["invocations"][0]["toolExecutionNotifications"];

            var templateErrorMessage = "An exception occurred while analyzing a template";
            Assert.AreEqual(templateErrorMessage, toolNotifications[0]["message"]["text"]);
            Assert.AreEqual(templateErrorMessage, toolNotifications[1]["message"]["text"]);

            var nonJsonFilePath1 = Path.Combine(directoryToAnalyze, "AnInvalidTemplate.json");
            var nonJsonFilePath2 = Path.Combine(directoryToAnalyze, "AnotherInvalidTemplate.json");
            var thirdNotificationMessageText = toolNotifications[2]["message"]["text"].ToString();
            // Both orders have to be considered for Windows and Linux:
            Assert.IsTrue($"Unable to analyze 2 file(s): {nonJsonFilePath1}, {nonJsonFilePath2}" == thirdNotificationMessageText ||
                $"Unable to analyze 2 file(s): {nonJsonFilePath2}, {nonJsonFilePath1}" == thirdNotificationMessageText);
            
            Assert.AreEqual("error", toolNotifications[0]["level"]);
            Assert.AreEqual("error", toolNotifications[1]["level"]);
            Assert.AreEqual("error", toolNotifications[2]["level"]);

            Assert.AreNotEqual(null, toolNotifications[0]["exception"]);
            Assert.AreNotEqual(null, toolNotifications[1]["exception"]);
            Assert.AreEqual(null, toolNotifications[2]["exception"]);
        }

        [DataTestMethod]
        [DataRow(false, DisplayName = "Outputs a recommendation for the verbose mode")]
        [DataRow(true, DisplayName = "Does not recommend the verbose mode")]
        public void AnalyzeDirectory_ExecutionWithErrorAndWarning_PrintsExpectedLogSummary(bool usesVerboseMode)
        {
            var directoryToAnalyze = GetFilePath("ToTestSummaryLogger");

            var expectedLogSummary = "2 error(s) and 1 warning(s) were found during the execution, please refer to the original messages above";

            if (!usesVerboseMode)
            {
                expectedLogSummary += $"{Environment.NewLine}The verbose mode (option -v or --verbose) can be used to obtain even more information about the execution";
            }
            
            expectedLogSummary += ($"{Environment.NewLine}Summary of the errors:" +
                $"{Environment.NewLine}\t1 instance(s) of: An exception occurred while analyzing a template" +
                $"{Environment.NewLine}\t1 instance(s) of: Unable to analyze 1 file(s): {Path.Combine(directoryToAnalyze, "ReportsError.json")}" +
                $"{Environment.NewLine}Summary of the warnings:" +
                $"{Environment.NewLine}\t1 instance(s) of: An exception occurred when processing the template language expressions{Environment.NewLine}");

            var args = new string[] { "analyze-directory", directoryToAnalyze };

            if (usesVerboseMode)
            {
                args = args.Append("--verbose").ToArray();
            }

            using StringWriter outputWriter = new();
            Console.SetOut(outputWriter);

            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            var cliConsoleOutput = outputWriter.ToString();
            var indexOfLogSummary = cliConsoleOutput.IndexOf("2 error(s) and 1 warning(s)");
            var logSummary = cliConsoleOutput[indexOfLogSummary..];

            Assert.AreEqual(expectedLogSummary, logSummary);
        }

        private static string GetFilePath(string testFileName)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Tests", testFileName);
        }
    }
}