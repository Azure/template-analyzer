// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
        [DataRow(@"{ ""property1"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.MachineLearningServices/workspaces", 0, 0, DisplayName = "0 matching resources with no results")]
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


        const string SimpleNestedFailExpectedSourceLocations = @"
            SimpleNestedFailModule.bicep:12, SimpleNestedFail.bicep:4
            SimpleNestedFailModule.bicep:16, SimpleNestedFail.bicep:4
            SimpleNestedFailModule.bicep:20, SimpleNestedFail.bicep:4
            SimpleNestedFailModule.bicep:24, SimpleNestedFail.bicep:4
            SimpleNestedFailModule.bicep:25, SimpleNestedFail.bicep:4
            SimpleNestedFailModule.bicep:26, SimpleNestedFail.bicep:4";
        const string DoubleNestedFailExpectedSourceLocations = @"
            DoubleNestedFailModule1.bicep:5, DoubleNestedFail.bicep:6
            DoubleNestedFailModule1.bicep:9, DoubleNestedFail.bicep:6
            DoubleNestedFailModule2.bicep:4, DoubleNestedFailModule1.bicep:13, DoubleNestedFail.bicep:6
            DoubleNestedFailModule2.bicep:8, DoubleNestedFailModule1.bicep:13, DoubleNestedFail.bicep:6
            DoubleNestedFailModule2.bicep:9, DoubleNestedFailModule1.bicep:13, DoubleNestedFail.bicep:6
            DoubleNestedFailModule2.bicep:10, DoubleNestedFailModule1.bicep:13, DoubleNestedFail.bicep:6";
        const string ParameterPassingFailExpectedSourceLocations = @"
            ParameterPassingFailModule.bicep:6, ParameterPassingFail.bicep:7
            ParameterPassingFailModule.bicep:10, ParameterPassingFail.bicep:7
            ParameterPassingFailModule.bicep:14, ParameterPassingFail.bicep:7
            ParameterPassingFailModule.bicep:18, ParameterPassingFail.bicep:7
            ParameterPassingFailModule.bicep:19, ParameterPassingFail.bicep:7";

        [DataTestMethod]
        [DataRow("SimpleNestedFail.json", new int[] { 29, 41, 47, 48, 53, 59, 60, 61 }, DisplayName = "Simple nested template example, outer scope with no collisions")]
        [DataRow("DoubleNestedFail.json", new int[] { 30, 36, 52, 58, 59,  60}, DisplayName = "Nested templates with two levels")]
        [DataRow("InnerOuterScopeFail.json", new int[] { 53, 59, 60, 105, 115, 116, 117 }, DisplayName = "Nested template with inner and outer scope, with colliding parameter names in parent and child templates")]
        [DataRow("SimpleNestedFail.bicep", SimpleNestedFailExpectedSourceLocations, DisplayName = "Simple bicep nested template example")]
        [DataRow("DoubleNestedFail.bicep", DoubleNestedFailExpectedSourceLocations, DisplayName = "Nested templates with two levels")]
        [DataRow("ParameterPassingFail.bicep", ParameterPassingFailExpectedSourceLocations, DisplayName = "Nested template with parameters passed from parent")]
        public void AnalyzeTemplate_ValidBicepModuleTemplate_ReturnsExpectedEvaluations(string templateFileName, string expectedSourceLocationsStr)
        {
            var templateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "templates");
            var filePath = Path.Combine(templateDirectory, templateFileName);
            var template = File.ReadAllText(filePath);

            var evaluations = templateAnalyzerSecurityRules.AnalyzeTemplate(template, templateFilePath: filePath);
            var failedSourceLocations = new List<List<(string fileName, int lineNumber)>>();

            foreach (var evaluation in evaluations)
            {
                if (!evaluation.Passed)
                {
                    foreach (var newLocation in GetFailedSourceLocations(evaluation))
                    {
                        // only add unique source locations
                        if (!failedSourceLocations.Any(existingLocation => existingLocation.SequenceEqual(newLocation)))
                        {
                            failedSourceLocations.Add(newLocation);
                        }
                    }
                }
            }

            var expectedSourceLocations = expectedSourceLocationsStr
                .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(locationStr => locationStr
                    .Split(",", StringSplitOptions.TrimEntries)
                    .Select(str => (
                        Path.Combine(templateDirectory, str.Split(":")[0]),
                        int.Parse(str.Split(":")[1]))).ToList()).ToList();

            Assert.AreEqual(expectedSourceLocations.Count, failedSourceLocations.Count);
            foreach(var expectedLocation in expectedSourceLocations)
            {
                Assert.IsTrue(failedSourceLocations.Any(loc => loc.SequenceEqual(expectedLocation)));
            }
        }

        private IEnumerable<List<(string fileName, int lineNumber)>> GetFailedSourceLocations(IEvaluation evaluation, ICollection<List<(string fileName, int lineNumber)>> failedSourceLocations = null)
        {
            failedSourceLocations ??= new List<List<(string fileName, int lineNumber)>>();

            if (!evaluation.Result?.Passed ?? false)
            {
                // build a list of file names/line numbers that are referenced
                var referenceList = new List<(string fileName, int lineNumber)>();
                var curLocation = evaluation.Result.SourceLocation;

                referenceList.Add((curLocation.FilePath, curLocation.LineNumber));
                while (curLocation.ReferencedBy != null)
                {
                    curLocation = curLocation.ReferencedBy;
                    referenceList.Add((curLocation.FilePath, curLocation.LineNumber));
                }

                failedSourceLocations.Add(referenceList);
            }

            foreach (var eval in evaluation.Evaluations.Where(e => !e.Passed))
            {
                GetFailedSourceLocations(eval, failedSourceLocations);
            }

            return failedSourceLocations;
        }

        [DataRow("SimpleNestedFail.json", new int[] { 36, 43, 46, 52, 53, 54 }, DisplayName = "Simple nested template example")]
        [DataRow("DoubleNestedFail.json", new int[] { 30, 36, 52, 58, 59, 60 }, DisplayName = "Nested templates with two levels")]
        [DataRow("InnerOuterScopeFail.json", new int[] { 49, 55, 56, 101, 107, 108, 109 }, DisplayName = "Nested template with inner and outer scope, with colliding parameter names in parent and child templates")]
        [DataRow("ParameterPassingFail.json", new int[] { 53, 59, 62, 68, 69 }, DisplayName = "Nested template with parameters passed from parent")]
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
                    failedEvaluationLines.UnionWith(GetFailedLines(evaluation));
                }
            }
            var expectedLineNumbers = new List<int>(lineNumbers);
            var failingLines = failedEvaluationLines.ToList();
            failingLines.Sort();

            Assert.AreEqual(expectedLineNumbers.Count, failingLines.Count);
            Assert.IsTrue(expectedLineNumbers.SequenceEqual(failingLines));
        }

        private IEnumerable<int> GetFailedLines(IEvaluation evaluation, HashSet<int> failedLines = null)
        {
            failedLines ??= new HashSet<int>();

            if (!evaluation.Result?.Passed ?? false)
            {
                failedLines.Add(evaluation.Result.SourceLocation.LineNumber);
            }

            foreach (var eval in evaluation.Evaluations.Where(e => !e.Passed))
            {
                GetFailedLines(eval, failedLines);
            }

            return failedLines;
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void FilterRules_ConfigurationNull_ExceptionThrown()
        {
            templateAnalyzerAllRules.FilterRules(null);
        }
    }
}
