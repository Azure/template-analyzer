// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Core.UnitTests
{
    [TestClass]
    public class TemplateAnalyzerTests
    {
        private static TemplateAnalyzer templateAnalyzerAllRules;
        private static TemplateAnalyzer templateAnalyzerSecurityRules;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            templateAnalyzerAllRules = TemplateAnalyzer.Create(true);
            templateAnalyzerSecurityRules = TemplateAnalyzer.Create(false);
        }

        [DataTestMethod]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, 1, DisplayName = "1 matching resource with 1 passing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.ServiceFabric/clusters", 1, 0, DisplayName = "1 matching resource with 1 failing evaluation")]
        [DataRow(@"{ ""property1"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.MachineLearningServices/workspaces", 2, 0, DisplayName = "2 matching resources with no results")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 2, 1, @"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", DisplayName = "2 matching resources with 1 passing evaluation")]
        public void AnalyzeTemplate_ValidInputValues_ReturnCorrectEvaluations(string resource1Properties, string resourceType, int expectedEvaluationCount, int expectedEvaluationPassCount, string resource2Properties = null)
        {
            string[] resourceProperties = { GenerateResource(resource1Properties, resourceType, "resource1"), GenerateResource(resource2Properties, resourceType, "resource2") };
            string template = GenerateTemplate(resourceProperties);

            var evaluations = templateAnalyzerSecurityRules.AnalyzeTemplate(template, "aFilePath");
            var evaluationsWithResults = evaluations.ToList().FindAll(evaluation => evaluation.HasResults); // EvaluateRulesAgainstTemplate will always return at least an evaluation for each built-in rule

            Assert.AreEqual(expectedEvaluationCount, evaluationsWithResults.Count);
            Assert.AreEqual(expectedEvaluationPassCount, evaluationsWithResults.Count(e => e.Passed));
        }

        [DataTestMethod]
        [DataRow("SimpleNestedFail.bicep", "{ \"SimpleNestedFailModule.bicep\": [16, 20, 21, 27, 31, 32, 33] }", DisplayName = "Simple bicep nested template example")]
        [DataRow("DoubleNestedFail.bicep", "{ \"DoubleNestedFailModule1.bicep\": [5, 9], \"DoubleNestedFailModule2.bicep\": [4, 8, 9, 10] }", DisplayName = "Nested templates with two levels")]
        [DataRow("ParameterPassingFail.bicep", "{ \"ParameterPassingFailModule.bicep\": [4, 8, 12, 16, 17] }", DisplayName = "Nested template with parameters passed from parent")]
        public void AnalyzeTemplate_ValidBicepModuleTemplate_ReturnsExpectedEvaluations(string templateFileName, dynamic expectedSourceLocationsJson)
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "templates");
            string filePath = Path.Combine(directory, templateFileName);
            string template = File.ReadAllText(filePath);

            var evaluations = templateAnalyzerSecurityRules.AnalyzeTemplate(template, templateFilePath: filePath);

            Dictionary<string, HashSet<int>> failedEvaluationSourceLocations = new();
            foreach (var evaluation in evaluations)
            {
                var failedResults = evaluation.GetFailedResults();
                foreach (var failedResult in failedResults)
                {
                    var resultFilePath = failedResult.SourceLocation.FilePath;
                    if (!failedEvaluationSourceLocations.ContainsKey(resultFilePath))
                    {
                        failedEvaluationSourceLocations[resultFilePath] = new();
                    }

                    failedEvaluationSourceLocations[resultFilePath].Add(failedResult.SourceLocation.LineNumber);
                }
            }

            Dictionary<string, int[]> expectedSourceLocations = JsonConvert.DeserializeObject<Dictionary<string, int[]>>(expectedSourceLocationsJson);
            foreach (var file in expectedSourceLocations.Keys)
            {
                var expectedLineNumbers = new List<int>(expectedSourceLocations[file]);
                var fullFilePath = Path.Combine(directory, file);
                var actualFailingLines = failedEvaluationSourceLocations[fullFilePath]?.ToList();
                actualFailingLines.Sort();

                Assert.AreEqual(expectedLineNumbers.Count, actualFailingLines.Count);
                Assert.IsTrue(expectedLineNumbers.SequenceEqual(actualFailingLines));
            }
        }

        [DataTestMethod]
        [DataRow("SimpleNestedFail.json", new int[] { 41, 47, 48, 53, 59, 60, 61 }, DisplayName = "Simple nested template example, outer scope with no collisions")]
        [DataRow("DoubleNestedFail.json", new int[] { 30, 36, 52, 58, 59, 60 }, DisplayName = "Nested templates with two levels")]
        [DataRow("InnerOuterScopeFail.json", new int[] { 53, 59, 60, 105, 115, 116, 117 }, DisplayName = "Nested template with inner and outer scope, with colliding parameter names in parent and child templates")]
        [DataRow("ParameterPassingFail.json", new int[] { 50, 56, 59, 65, 66 }, DisplayName = "Nested template with parameters passed from parent")]
        public void AnalyzeTemplate_ValidNestedTemplate_ReturnsExpectedEvaluations(string templateFileName, dynamic lineNumbers)
        {
            string filePath = Path.Combine("templates", templateFileName);
            string template = File.ReadAllText(filePath);

            var evaluations = templateAnalyzerSecurityRules.AnalyzeTemplate(template, "aFilePath");
            HashSet<int> failedEvaluationLines = new();

            foreach (var evaluation in evaluations)
            {
                if (!evaluation.Passed)
                {
                    var failedLines = evaluation.GetFailedResults().Select(r => r.SourceLocation.LineNumber);
                    failedEvaluationLines.UnionWith(failedLines);
                }
            }
            var expectedLineNumbers = new List<int>(lineNumbers);
            var failingLines = failedEvaluationLines.ToList();
            failingLines.Sort();

            Assert.AreEqual(expectedLineNumbers.Count, failingLines.Count);
            Assert.IsTrue(expectedLineNumbers.SequenceEqual(failingLines));
        }

        [TestMethod]
        public void AnalyzeTemplate_RunningAllRules_ReturnsMoreEvaluations()
        {
            string[] resourceProperties = {
                GenerateResource(
                    @"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantIdValue"" } }",
                    "Microsoft.ServiceFabric/clusters", "resource1")
            };
            string template = GenerateTemplate(resourceProperties);

            var securityEvaluations = templateAnalyzerSecurityRules.AnalyzeTemplate(template, "aFilePath");
            var allEvaluations = templateAnalyzerAllRules.AnalyzeTemplate(template, "aFilePath");

            Assert.IsTrue(securityEvaluations.Count() < allEvaluations.Count());
        }

        private string GenerateTemplate(string[] resourceProperties)
        {
            return string.Format(@"{{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""resources"": [ {0} ] }}", string.Join(',', resourceProperties));
        }

        private string GenerateResource(string resourceProperties, string resourceType, string resourceName)
        {
            if (string.IsNullOrEmpty(resourceProperties))
            {
                return null;
            }

            return string.Format(@"
                    {{
                      ""apiVersion"": ""2018-02-01"",
                      ""name"": ""{2}"",
                      ""type"": ""{1}"",
                      ""properties"": {0}
                    }}", resourceProperties, resourceType, resourceName);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AnalyzeTemplate_TemplateIsNull_ThrowArgumentNullException()
        {
            templateAnalyzerAllRules.AnalyzeTemplate(null, "aFilePath");
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateAnalyzerException))]
        public void AnalyzeTemplate_JsonTemplateIsInvalid_ThrowTemplateAnalyzerException()
        {
            templateAnalyzerAllRules.AnalyzeTemplate("{}", "aFilePath");
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateAnalyzerException))]
        public void AnalyzeTemplate_BicepTemplateIsInvalid_ThrowTemplateAnalyzerException()
        {
            var invalidBicep = "param location string = badString";
            var templateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "invalid.bicep");

            try
            {
                File.WriteAllText(templateFilePath, invalidBicep);
                templateAnalyzerAllRules.AnalyzeTemplate(invalidBicep, templateFilePath: templateFilePath);
            }
            finally
            {
                File.Delete(templateFilePath);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AnalyzeTemplate_MissingFilePath_ThrowTemplateAnalyzerException()
        {
            templateAnalyzerAllRules.AnalyzeTemplate(@"
                {
                  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                  ""contentVersion"": ""1.0.0.0"",
                  ""resources"": []
                }", null);
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateAnalyzerException))]
        public void Create_MissingRulesFile_ThrowsException()
        {
            var rulesDir = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Rules");
            var rulesFile = Path.Combine(rulesDir, "BuiltInRules.json");
            var movedFile = Path.Combine(rulesDir, "MovedRules.json");

            // Move rules file
            File.Move(rulesFile, movedFile);

            try
            {
                TemplateAnalyzer.Create(false);
            }
            finally
            {
                File.Move(movedFile, rulesFile, overwrite: true);
            }
        }

        [TestMethod]
        public void FilterRules_ValidConfiguration_NoExceptionThrown()
        {
            // Note: this is not a great test, but there's very little to validate in this function.
            TemplateAnalyzer.Create(false).FilterRules(new ConfigurationDefinition());
        }

        [TestMethod]
        public void AnalyzeTemplate_NoPowershellRulesRunning_ReturnsLessEvaluations()
        {
            string[] resourceProperties = {
                GenerateResource(
                    @"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantIdValue"" } }",
                    "Microsoft.ServiceFabric/clusters", "resource1")
            };
            string template = GenerateTemplate(resourceProperties);

            var noPowershellEvaluations = TemplateAnalyzer.Create(includeNonSecurityRules: true, includePowerShellRules: false).AnalyzeTemplate(template, "aFilePath");
            var allRules = templateAnalyzerAllRules.AnalyzeTemplate(template, "aFilePath");

            Assert.IsTrue(noPowershellEvaluations.Count() < allRules.Count());
        }

        [TestMethod]
        public void CustomRulesFileIsProvided_NoExceptionThrown()
        {
            var rulesDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Rules");
            var rulesFile = Path.Combine(rulesDir, "BuiltInRules.json");
            var movedFile = Path.Combine(rulesDir, "MovedRules.json");

            // Move rules file
            File.Move(rulesFile, movedFile, overwrite: true);

            var customRulesFile = new FileInfo(movedFile);

            try
            {
                TemplateAnalyzer.Create(false, null, customRulesFile);
            }
            finally
            {
                File.Move(movedFile, rulesFile, overwrite: true);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FilterRules_ConfigurationNull_ExceptionThrown()
        {
            templateAnalyzerAllRules.FilterRules(null);
        }
    }
}
