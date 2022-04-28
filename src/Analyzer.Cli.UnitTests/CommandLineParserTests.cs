// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Templates.Analyzer.Cli;
using System.Linq;
using System;
using System.IO;

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
        [DataRow("\\CommandLineParserTests.cs", 4, DisplayName = "Path exists, not an ARM template.")]
        [DataRow("\\CommandLineParserTests.cs", 3, "--report-format", "Sarif", DisplayName = "Path exists, Report-format flag set, --output-file-path flag not included.")]
        [DataRow("\\CommandLineParserTests.cs", 1, "--parameters-file-path", DisplayName = "Path exists, Parameters-file-path flag included, but no value provided.")]
        [DataRow("\\AppServicesLogs-Failures.json", 5, DisplayName = "Issues found in the template")]
        [DataRow("\\AppServicesLogs-Passes.json", 0, DisplayName = "Success")]
        public void AnalyzeTemplate_ValidInputValues_ReturnExitCode(string relativeTemplatePath, int expectedExitCode, params string[] additionalParams)
        {
            var args = new string[] { "analyze-template" , String.Concat(Directory.GetCurrentDirectory(), relativeTemplatePath)}; 
            args = args.Concat(additionalParams).ToArray();
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(expectedExitCode, result.Result);
        }

        [DataTestMethod]
        [DataRow("\\Configuration.json", 1, DisplayName = "Provided parameters file is not a parameters file")]
        [DataRow("\\Parameters.json", 5, DisplayName = "Provided parameters file correct, issues in template")]
        public void AnalyzeTemplate_ParameterFileParamUsed_ReturnExitCode(string relativeParametersFilePath, int expectedExitCode)
        {
            var templatePath = String.Concat(Directory.GetCurrentDirectory(), "\\AppServicesLogs-Failures.json");
            var parametersFilePath = String.Concat(Directory.GetCurrentDirectory(), relativeParametersFilePath);
            var args = new string[] { "analyze-template", templatePath, "--parameters-file-path", parametersFilePath };
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(expectedExitCode, result.Result);
        }

        [TestMethod]
        public void AnalyzeTemplate_ConfigurationFileParamUsed_ReturnExitCode()
        {
            var templatePath = String.Concat(Directory.GetCurrentDirectory(), "\\AppServicesLogs-Failures.json");
            var configurationPath = String.Concat(Directory.GetCurrentDirectory(), "\\Configuration.json");
            var args = new string[] { "analyze-template", templatePath, "--config-file-path", configurationPath};
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(5, result.Result);
        }

        [TestMethod]
        public void AnalyzeTemplate_SarifRun_ReturnExitCode()
        {
            var templatePath = String.Concat(Directory.GetCurrentDirectory(), "\\AppServicesLogs-Failures.json");
            var outputFilePath = String.Concat(Directory.GetCurrentDirectory(), "\\outPutFile.sarif");
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
        [DataRow(true, 6, DisplayName = "Error + Issue: Scan has both errors and issues")]
        public void AnalyzeDirectory_ValidInputValues_ReturnExpectedExitCode(bool useTestDirectoryPath, int expectedExitCode, params string[] additionalParams)
        {
            string[] args;
            if (useTestDirectoryPath)
                args = new string[] { "analyze-directory", Directory.GetCurrentDirectory() };
            else
                args = new string[] { "analyze-directory", "Directory does not exist" };

            args = args.Concat(additionalParams).ToArray();
            var result = _commandLineParser.InvokeCommandLineAPIAsync(args);

            Assert.AreEqual(expectedExitCode, result.Result);
        }
    }
}
