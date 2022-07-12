// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Templates.Analyzer.Cli;
using Microsoft.Azure.Templates.Analyzer.Types;
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
        [DataRow("Path does not exist", ExitCode.ErrorInvalidPath, DisplayName = "Invalid template file path provided")]
        [DataRow("Configuration.json", ExitCode.ErrorInvalidARMTemplate, DisplayName = "Path exists, not an ARM template.")]
        [DataRow("Configuration.json", ExitCode.ErrorMissingPath, "--report-format", "Sarif", DisplayName = "Path exists, Report-format flag set, --output-file-path flag not included.")]
        [DataRow("Configuration.json", ExitCode.ErrorCommand, "--parameters-file-path", DisplayName = "Path exists, Parameters-file-path flag included, but no value provided.")]
        [DataRow("AppServicesLogs-Failures.json", ExitCode.Violation, DisplayName = "Violations found in the template")]
        [DataRow("AppServicesLogs-Passes.json", ExitCode.Success, DisplayName = "Success")]
        [DataRow("AppServicesLogs-Failures.bicep", ExitCode.Violation, DisplayName = "Violations found in the Bicep template")]
        [DataRow("AppServicesLogs-Passes.bicep", ExitCode.Success, DisplayName = "Success")]
        [DataRow("Invalid.bicep", ExitCode.ErrorInvalidBicepTemplate, DisplayName = "Path exists, invalid Bicep template")]
        public void AnalyzeTemplate_ValidInputValues_ReturnExpectedExitCode(string relativeTemplatePath, ExitCode expectedExitCode, params string[] additionalCliOptions)
        {
            var args = new string[] { "analyze-template" , GetFilePath(relativeTemplatePath)}; 
            args = args.Concat(additionalCliOptions).ToArray();
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual((int)expectedExitCode, result.Result);
        }

        [DataTestMethod]
        [DataRow("Configuration.json", ExitCode.ErrorAnalysis, DisplayName = "Provided parameters file is not a parameters file")]
        [DataRow("Parameters.json", ExitCode.Violation, DisplayName = "Provided parameters file correct, issues in template")]
        public void AnalyzeTemplate_ParameterFileParamUsed_ReturnExpectedExitCode(string relativeParametersFilePath, ExitCode expectedExitCode)
        {
            var templatePath = GetFilePath("AppServicesLogs-Failures.json");
            var parametersFilePath = GetFilePath(relativeParametersFilePath);
            var args = new string[] { "analyze-template", templatePath, "--parameters-file-path", parametersFilePath };
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual((int)expectedExitCode, result.Result);
        }

        [DataTestMethod]
        [DataRow("AppServicesLogs-Failures.json")]
        [DataRow("AppServicesLogs-Failures.bicep")]
        public void AnalyzeTemplate_UseConfigurationFileOption_ReturnExpectedExitCodeUsingOption(string relativeTemplatePath)
        {
            var templatePath = GetFilePath(relativeTemplatePath);
            var configurationPath = GetFilePath("Configuration.json");
            var args = new string[] { "analyze-template", templatePath, "--config-file-path", configurationPath};
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual((int)ExitCode.Violation, result.Result);
        }

        [DataTestMethod]
        [DataRow("AppServicesLogs-Failures.json")]
        [DataRow("AppServicesLogs-Failures.bicep")]
        public void AnalyzeTemplate_ReportFormatAsSarif_ReturnExpectedExitCodeUsingOption(string relativeTemplatePath)
        {
            var templatePath = GetFilePath(relativeTemplatePath);
            var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "OutputFile.sarif");
            var args = new string[] { "analyze-template", templatePath, "--report-format", "Sarif", "--output-file-path", outputFilePath };
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual((int)ExitCode.Violation, result.Result);
            
            File.Delete(outputFilePath);
        }

        [TestMethod]
        public void AnalyzeDirectory_ValidInputValues_AnalyzesExpectedNumberOfFiles()
        {
            var args = new string[] { "analyze-directory", Directory.GetCurrentDirectory() };

            using StringWriter outputWriter = new();
            Console.SetOut(outputWriter);

            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual((int)ExitCode.ErrorAndViolation, result.Result);
            StringAssert.Contains(outputWriter.ToString(), "Analyzed 8 files");
        }

        [DataTestMethod]
        [DataRow(false, ExitCode.ErrorInvalidPath, DisplayName = "Invalid directory path provided")]
        [DataRow(true, ExitCode.ErrorMissingPath, "--report-format", "Sarif", DisplayName = "Directory exists, Report-format flag set, --output-file-path flag not included.")]
        [DataRow(true, ExitCode.ErrorCommand, "--report-format", "Console", "--output-file-path", DisplayName = "Path exists, Report-format flag set, --output-file-path flag included, but no value provided.")]
        [DataRow(true, ExitCode.ErrorAndViolation, DisplayName = "Error + Violation: Scan has both errors and violations")]
        public void AnalyzeDirectory_ValidInputValues_ReturnExpectedExitCode(bool useTestDirectoryPath, ExitCode expectedExitCode, params string[] additionalCliOptions)
        {
            var args = new string[] { "analyze-directory", useTestDirectoryPath ? Directory.GetCurrentDirectory() : "Directory does not exist" };

            args = args.Concat(additionalCliOptions).ToArray();
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);
            
            Assert.AreEqual((int)expectedExitCode, result.Result);
        }

        [TestMethod]
        public void AnalyzeDirectory_DirectoryWithInvalidTemplates_LogsExpectedErrorInSarif()
        {
            var outputFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Output.sarif");
            var directoryToAnalyze = GetFilePath("ToTestSarifNotifications");
            
            var args = new string[] { "analyze-directory", directoryToAnalyze, "--report-format", "Sarif", "--output-file-path", outputFilePath };

            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual((int)ExitCode.ErrorAnalysis, result.Result);

            var sarifOutput = JObject.Parse(File.ReadAllText(outputFilePath));
            var toolNotifications = sarifOutput["runs"][0]["invocations"][0]["toolExecutionNotifications"];

            var templateErrorMessage = "An exception occurred while analyzing a template";
            Assert.AreEqual(templateErrorMessage, toolNotifications[0]["message"]["text"]);
            Assert.AreEqual(templateErrorMessage, toolNotifications[1]["message"]["text"]);

            var nonJsonFilePath1 = Path.Combine(directoryToAnalyze, "AnInvalidTemplate.json");
            var nonJsonFilePath2 = Path.Combine(directoryToAnalyze, "AnInvalidTemplate.bicep");
            var thirdNotificationMessageText = toolNotifications[2]["message"]["text"].ToString();
            // Both orders have to be considered for Windows and Linux:
            Assert.IsTrue($"Unable to analyze 2 files: {nonJsonFilePath1}, {nonJsonFilePath2}" == thirdNotificationMessageText ||
                $"Unable to analyze 2 files: {nonJsonFilePath2}, {nonJsonFilePath1}" == thirdNotificationMessageText);
            
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
        [DataRow(false, true, DisplayName = "Outputs a recommendation for the verbose mode and uses plural form for 'errors'")]
        public void AnalyzeDirectory_ExecutionWithErrorAndWarning_PrintsExpectedLogSummary(bool usesVerboseMode, bool multipleErrors = false)
        {
            var directoryToAnalyze = GetFilePath("ToTestSummaryLogger");

            var expectedLogSummary = "Analysis output:";

            if (!usesVerboseMode)
            {
                expectedLogSummary += $"{Environment.NewLine}\tThe verbose mode (option -v or --verbose) can be used to obtain even more information about the execution.";
            }
            
            expectedLogSummary += ($"{Environment.NewLine}{Environment.NewLine}\tSummary of the warnings:" +
                $"{Environment.NewLine}\t\t1 instance of: An exception occurred when processing the template language expressions{Environment.NewLine}") +
                $"{Environment.NewLine}\tSummary of the errors:" +
                $"{Environment.NewLine}\t\t{(multipleErrors ? "2 instances" : "1 instance")} of: An exception occurred while analyzing a template";
            
            expectedLogSummary += ($"{Environment.NewLine}{Environment.NewLine}\t1 Warning" +
                $"{Environment.NewLine}\t{(multipleErrors ? "2 Errors" : "1 Error")}{Environment.NewLine}");

            var args = new string[] { "analyze-directory", directoryToAnalyze };

            if (usesVerboseMode)
            {
                args = args.Append("--verbose").ToArray();
            }

            using StringWriter outputWriter = new();
            Console.SetOut(outputWriter);

            // Copy template producing an error to get multiple errors in run
            string secondErrorTemplate = Path.Combine(directoryToAnalyze, "ReportsError2.json");
            if (multipleErrors)
            {
                File.Copy(Path.Combine(directoryToAnalyze, "ReportsError.json"), secondErrorTemplate);
            }

            try
            {
                var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

                var cliConsoleOutput = outputWriter.ToString();
                var indexOfLogSummary = cliConsoleOutput.IndexOf("Analysis output:");
                Assert.IsTrue(indexOfLogSummary >= 0, $"Expected log message not found in CLI output.  Found:{Environment.NewLine}{cliConsoleOutput}");
                var logSummary = cliConsoleOutput[indexOfLogSummary..];

                Assert.AreEqual(expectedLogSummary, logSummary);
            }
            finally
            {
                File.Delete(secondErrorTemplate);
            }
        }

        [DataTestMethod]
        [DataRow(false, "myConfig.json", true, DisplayName = "Custom config name specified in command")]
        [DataRow(false, "configuration.json", false, DisplayName = "Config not specified, default config path applied")]
        [DataRow(true, "myConfig.json", true, DisplayName = "Custom config name specified in command")]
        [DataRow(true, "configuration.json", false, DisplayName = "Config not specified, default config path applied")]
        public void FilterRules_ValidConfig_RulesFiltered(bool isBicep, string configName, bool specifyInCommand)
        {
            var extension = isBicep ? "bicep" : "json";
            var templatePath = GetFilePath($"AppServicesLogs-Failures.{extension}");
            var args = new string[] { "analyze-template", templatePath };

            // Analyze template without filtering rules to verify there is a failure.
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);
            Assert.AreEqual((int)ExitCode.Violation, result.Result);

            // Run again with rule filtered out, verify it passes (config was loaded).
            try
            {
                File.WriteAllText(configName,
                    JObject.FromObject(
                        new ConfigurationDefinition
                        {
                            InclusionsConfigurationDefinition = new()
                            {
                                // Use a non-existent rule id so all rules are filtered out.
                                Ids = new() { "NonRuleId" }
                            }
                        })
                    .ToString());
                
                if (specifyInCommand)
                    args = args.Concat(new[] { "--config-file-path", configName }).ToArray();

                result = _commandLineParser.InvokeCommandLineAPIAsync(args);
                Assert.AreEqual((int)ExitCode.Success, result.Result);
            }
            finally
            {
                File.Delete(configName);
            }
        }

        [DataTestMethod]
        [DataRow("AppServicesLogs-Failures.json")]
        [DataRow("AppServicesLogs-Failures.bicep")]
        public void FilterRules_ConfigurationPathIsInvalid_ReturnsConfigurationError(string relativeTemplatePath)
        {
            var templatePath = GetFilePath(relativeTemplatePath);
            var args = new string[] { "analyze-template", templatePath, "--config-file-path", "NonExistentFile.json" };

            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);
            Assert.AreEqual((int)ExitCode.ErrorInvalidConfiguration, result.Result);
        }

        [DataTestMethod]
        [DataRow("myconfig.json", "", true, DisplayName = "Empty config file specified")]
        [DataRow("configuration.json", "", false, DisplayName = "Empty default config file")]
        [DataRow("myconfig.json", "Invalid JSON", true, DisplayName = "Malformed config file specified")]
        [DataRow("configuration.json", "Invalid JSON", false, DisplayName = "Malformed default config file")]
        public void FilterRules_InvalidConfigurationFile_ReturnsConfigurationError(string configPath, string configContents, bool specifyInCommand)
        {
            var templatePath = GetFilePath("AppServicesLogs-Passes.json");
            var args = new string[] { "analyze-template", templatePath };
            if (specifyInCommand)
                args = args.Concat(new[] { "--config-file-path", configPath }).ToArray();

            try
            {
                File.WriteAllText(configPath, configContents);
                var result = _commandLineParser.InvokeCommandLineAPIAsync(args);
                Assert.AreEqual((int)ExitCode.ErrorInvalidConfiguration, result.Result);
            }
            finally
            {
                File.Delete(configPath);
            }
        }

        private static string GetFilePath(string testFileName)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "Tests", testFileName);
        }

        [DataTestMethod]
        [DataRow(TestcaseTemplateConstants.PassingTest, ExitCode.Success, DisplayName = "Valid Template")]
        [DataRow(TestcaseTemplateConstants.SchemaCaseInsensitive, ExitCode.Success, DisplayName = "Schema is case insensitive")]
        [DataRow(TestcaseTemplateConstants.DifferentSchemaDepths, ExitCode.Success, DisplayName = "Two schemas, different depths, valid schema last")]
        [DataRow(TestcaseTemplateConstants.MissingStartObject, ExitCode.ErrorInvalidARMTemplate, DisplayName = "Missing start object")]
        [DataRow(TestcaseTemplateConstants.NoValidTopLevelProperties, ExitCode.ErrorInvalidARMTemplate, DisplayName = "Invalid property depths")]
        [DataRow(TestcaseTemplateConstants.MissingSchema, ExitCode.ErrorInvalidARMTemplate, DisplayName = "Missing schema, capitalized property names")]
        [DataRow(TestcaseTemplateConstants.SchemaValueNotString, ExitCode.ErrorInvalidARMTemplate, DisplayName = "Schema value isn't string")]
        [DataRow(TestcaseTemplateConstants.NoSchemaInvalidProperties, ExitCode.ErrorInvalidARMTemplate, DisplayName = "No schema, invalid properties")]
        public void IsValidTemplate_ValidAndInvalidInputTemplates_ReturnExpectedErrorCode(string templateToAnalyze, ExitCode expectedErrorCode)
        {
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "output.json");

            try
            {
                File.WriteAllText(templatePath, templateToAnalyze);
                var args = new string[] { "analyze-template", templatePath };
                var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

                Assert.AreEqual((int)expectedErrorCode, result.Result);
            }
            finally
            {
                File.Delete(templatePath);
            }
        }
    }
}