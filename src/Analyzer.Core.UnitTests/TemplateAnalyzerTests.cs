// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Azure.ResourceManager.Resources.Models;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.ResourceStack.Common.Storage;
using Newtonsoft.Json;
using Powershell = System.Management.Automation.PowerShell; // There's a conflict between this class name and a namespace

namespace Microsoft.Azure.Templates.Analyzer.Core.UnitTests
{
    [TestClass]
    public class TemplateAnalyzerTests
    {
        private static TemplateAnalyzer templateAnalyzer;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            templateAnalyzer = TemplateAnalyzer.Create(usePowerShell: true);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var powerShell = Powershell.Create();

                powerShell.Commands.AddCommand("Set-ExecutionPolicy")
                    .AddParameter("Scope", "Process") // Affects only the current PowerShell session
                    .AddParameter("ExecutionPolicy", "Unrestricted");

                powerShell.Invoke();
            }
        }

        [DataTestMethod]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, 1, DisplayName = "1 matching Resource with 1 passing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.ServiceFabric/clusters", 1, 0, DisplayName = "1 matching Resource with 1 failing evaluation")]
        [DataRow(@"{ ""property1"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.Storage/storageAccounts", 0, 0, DisplayName = "0 matching Resources with no results")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 2, 1, @"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", DisplayName = "2 matching Resources with 1 passing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, 1, null, @"..\..\..\..\Analyzer.PowerShellRuleEngine.UnitTests\templates\success.json", DisplayName = "1 matching Resource with 1 passing evaluation, specifying a template file path")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.ServiceFabric/clusters", 2, 0, null, @"..\..\..\..\Analyzer.PowerShellRuleEngine.UnitTests\templates\error_without_line_number.json", DisplayName = "1 matching Resource with 2 failing evaluations, specifying a template file path")]
        public void AnalyzeTemplate_ValidInputValues_ReturnCorrectEvaluations(string resource1Properties, string resourceType, int expectedEvaluationCount, int expectedEvaluationPassCount, string resource2Properties = null, string templateFilePath = null)
        {
            // Arrange
            string[] resourceProperties = { GenerateResource(resource1Properties, resourceType, "resource1"), GenerateResource(resource2Properties, resourceType, "resource2") };
            string template = GenerateTemplate(resourceProperties);

            var evaluations = templateAnalyzer.AnalyzeTemplate(template, templateFilePath: templateFilePath);
            var evaluationsWithResults = evaluations.ToList().FindAll(evaluation => evaluation.HasResults); // EvaluateRulesAgainstTemplate will always return at least an evaluation for each built-in rule

            Assert.AreEqual(expectedEvaluationCount, evaluationsWithResults.Count);
            Assert.AreEqual(expectedEvaluationPassCount, evaluationsWithResults.Count(e => e.Passed));
        }


        //  -----------------------------------------------------------------------

        [DataTestMethod]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, 1, "outer", DisplayName = "1 matching Resource with 1 passing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.ServiceFabric/clusters", 1, 0, "inner", DisplayName = "1 matching Resource with 1 failing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""[parameters('tenantId')]"" } }", "Microsoft.ServiceFabric/clusters", 1, 1, "outer", @"{ ""tenantId"": { ""type"": ""string"", ""defaultValue"": ""tenantId"" } }", DisplayName = "1 matching Resource with 1 passing evaluation")]
        [DataRow(@"{ ""type"":  ""[parameters('CustomRole')]"" }", "Microsoft.Authorization/roleDefinitions", 1, 0, null, @"{ ""CustomRole"": { ""type"": ""string"", ""defaultValue"": ""CustomRole"" } }", DisplayName = "1 matching Resource with 1 failing evaluation")]
        [DataRow(@"{ ""type"":  ""[parameters('CustomRole')]"" }", "Microsoft.Authorization/roleDefinitions", 1, 1, "inner", @"{ ""CustomRole"": { ""type"": ""string"", ""defaultValue"": ""CustomRole"" } }", DisplayName = "1 matching Resource with 1 passing evaluation")]
        
        public void AnalyzeTemplate_ValidInputValues_ReturnCorrectEvaluations_NestedTemplates(string resource1Properties, string resourceType, int expectedEvaluationCount, int expectedEvaluationPassCount, string evaluationScope = null, string parameters = null, string passedParameters = null, string resource2Properties = null, string templateFilePath = null)
        {
            // Arrange
            string[] resourceProperties = { GenerateResource(resource1Properties, resourceType, "resource1"), GenerateResource(resource2Properties, resourceType, "resource2") };
            string template = GenerateTemplate(resourceProperties);  // child template
            string parentTemplate = GenerateParentTemplate(evaluationScope, template, parameters, passedParameters);  // parent template

            var evaluations = templateAnalyzer.AnalyzeTemplate(parentTemplate, templateFilePath: templateFilePath);
            var evaluationsWithResults = evaluations.ToList().FindAll(evaluation => evaluation.HasResults); // EvaluateRulesAgainstTemplate will always return at least an evaluation for each built-in rule

            Assert.AreEqual(expectedEvaluationCount, evaluationsWithResults.Count);
            Assert.AreEqual(expectedEvaluationPassCount, evaluationsWithResults.Count(e => e.Passed));

            string doubleNestedTemplate = GenerateParentTemplate(evaluationScope, parentTemplate, parameters, passedParameters);

            var doubleEvaluations = templateAnalyzer.AnalyzeTemplate(doubleNestedTemplate, templateFilePath: templateFilePath);
            var doubleEvaluationsWithResults = evaluations.ToList().FindAll(doubleEvaluation => doubleEvaluation.HasResults); // EvaluateRulesAgainstTemplate will always return at least an evaluation for each built-in rule

            Assert.AreEqual(expectedEvaluationCount * 2, evaluationsWithResults.Count * 2);
            Assert.AreEqual(expectedEvaluationPassCount * 2, evaluationsWithResults.Count(e => e.Passed) * 2);

        }

        private string GenerateParentTemplate(string evaluationScope, string childTemplate, string parameters = null, string passedParameters = null)
        {
            if (passedParameters != null)
            {
                dynamic childTemplateObject = JsonConvert.DeserializeObject(childTemplate);
                childTemplateObject.parameters = null ;

            }
            string scope = string.Format(@"
                    {{
                        ""scope"": ""{0}""
                    }}", evaluationScope);

            string properties = string.Format(@"
                        {{
                          ""parameters"": {2},
                          ""mode"": ""Incremental"",
                          ""expressionEvaluationOptions"": {1},
                          ""template"": {0}
                        }}", childTemplate, scope, passedParameters != null ? passedParameters : "{}");

            string resources = string.Format(@"
            {{
            ""type"": ""Microsoft.Resources/deployments"",
            ""apiVersion"": ""2019-10-01"",
            ""name"": ""nestedTemplate"",
            ""resourceGroup"": ""my-rg"",
            ""properties"": {0}
            }}", string.Join(',', properties));

            // construct parent of nested template
            return string.Format(@"
            {{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""parameters"": {0},
            ""resources"": [ {1} ] }}", parameters != null? parameters : "{}", resources);  //string.Join(',', resourceProperties
        }


        //-------------------- good tests
        [DataTestMethod]
        [DataRow("SimpleNestedFail.json", new int[] { 43, 54 }, DisplayName = "")]
        [DataRow("DoubleNestedFail.json", new int[] { 37, 61}, DisplayName = "")]
        [DataRow("InnerOuterScopeFail.json", new int[] { 109 }, DisplayName = "")]
        [DataRow("ParameterPassingFail.json", new int[] { 59 }, DisplayName = "")]
        public void AnalyzeTemplate_ValidNestedTemplate_ReturnsExpectedEvaluations(string templateFileName, dynamic lineNumbers, string templateFilePath = null)
        {
            string filePath = new(Path.Combine(".", "templates", templateFileName));
            StreamReader sr = new StreamReader(filePath);
            string template = sr.ReadToEnd();

            var evaluations = templateAnalyzer.AnalyzeTemplate(template, templateFilePath: templateFilePath);
            HashSet<int> failedEvaluationLines = new HashSet<int>();

            foreach (var evaluation in evaluations)
            {
                failedEvaluationLines.UnionWith(GetFailedLines(evaluation));
            }
            foreach(int lineNumber in lineNumbers)
            {
                Assert.IsTrue(failedEvaluationLines.Contains(lineNumber));
            }
        }

        private HashSet<int> GetFailedLines(IEvaluation evaluation, HashSet<int> failedLines = null)
        {
            failedLines ??= new HashSet<int>();

            if (!evaluation.Result?.Passed ?? false)
            {
                failedLines.Add(evaluation.Result.LineNumber);
            }

            foreach (var eval in evaluation.Evaluations.Where(e => !e.Passed))
            {
                GetFailedLines(eval, failedLines);
            }

            return failedLines;
        }
        //  -----------------------------------------------------------------------


        [TestMethod]
        public void AnalyzeTemplate_NotUsingPowerShell_NoPowerShellViolations()
        {
            // Arrange
            string[] resourceProperties = {
                GenerateResource(
                    @"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantIdValue"" } }",
                    "Microsoft.ServiceFabric/clusters", "resource1")
            };
            string template = GenerateTemplate(resourceProperties);

            // Analyze with PowerShell disabled
            var templateAnalyzerWithoutPowerShell = TemplateAnalyzer.Create(usePowerShell: false);
            var evaluations = templateAnalyzerWithoutPowerShell.AnalyzeTemplate(
                template,
                templateFilePath: @"..\..\..\..\Analyzer.PowerShellRuleEngine.UnitTests\templates\error_without_line_number.json"); // This file has violations from PowerShell rules

            // There should be no PowerShell rule evaluations because the PowerShell engine should not have run
            Assert.IsFalse(evaluations.Any(e => e is PowerShellRuleEvaluation));

            // Analyze with PowerShell enabled
            evaluations = templateAnalyzer.AnalyzeTemplate(
                template,
                templateFilePath: @"..\..\..\..\Analyzer.PowerShellRuleEngine.UnitTests\templates\error_without_line_number.json"); // This file has violations from PowerShell rules

            // There should be at least one PowerShell rule evaluation because the PowerShell engine should have run
            Assert.IsTrue(evaluations.Any(e => e is PowerShellRuleEvaluation));
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
            templateAnalyzer.AnalyzeTemplate(null);
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateAnalyzerException))]
        public void AnalyzeTemplate_JsonTemplateIsInvalid_ThrowTemplateAnalyzerException()
        {
            templateAnalyzer.AnalyzeTemplate("{}");
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
                templateAnalyzer.AnalyzeTemplate(invalidBicep, templateFilePath: templateFilePath);
            }
            finally
            {
                File.Delete(templateFilePath);
            }
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
            templateAnalyzer.FilterRules(null);
        }
    }
}