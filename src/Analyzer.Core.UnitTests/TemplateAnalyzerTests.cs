// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            templateAnalyzer = TemplateAnalyzer.Create();
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
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, true, DisplayName = "1 matching Resource with 1 passing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.ServiceFabric/clusters", 1, false, DisplayName = "1 matching Resource with 1 failing evaluation")]
        [DataRow(@"{ ""property1"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.Storage/storageAccounts", 0, false, DisplayName = "0 matching Resources with no results")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, false, @"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", DisplayName = "2 matching Resources with 1 passing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, true, null, @"..\..\..\..\Analyzer.PowerShellRuleEngine.UnitTests\templates\success.json", DisplayName = "1 matching Resource with 1 passing evaluation, specifying a template file path")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.ServiceFabric/clusters", 2, false, null, @"..\..\..\..\Analyzer.PowerShellRuleEngine.UnitTests\templates\error_without_line_number.json", DisplayName = "1 matching Resource with 2 failing evaluations, specifying a template file path")]
        public void AnalyzeTemplate_ValidInputValues_ReturnCorrectEvaluations(string resource1Properties, string resourceType, int expectedEvaluationCount, bool expectedEvaluationPassed, string resource2Properties = null, string templateFilePath = null)
        {
            // Arrange
            string[] resourceProperties = { GenerateResource(resource1Properties, resourceType, "resource1"), GenerateResource(resource2Properties, resourceType, "resource2") };
            string template = GenerateTemplate(resourceProperties);

            var evaluations = templateAnalyzer.AnalyzeTemplate(template, templateFilePath: templateFilePath);
            var evaluationsWithResults = evaluations.ToList().FindAll(evaluation => evaluation.HasResults); // EvaluateRulesAgainstTemplate will always return at least an evaluation for each built-in rule

            Assert.AreEqual(expectedEvaluationCount, evaluationsWithResults.Count);

            foreach(IEvaluation evaluation in evaluationsWithResults)
            {
                Assert.AreEqual(expectedEvaluationPassed, evaluation.Passed);
            }
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
        public void AnalyzeTemplate_TemplateIsInvalid_ThrowTemplateAnalyzerException()
        {
            templateAnalyzer.AnalyzeTemplate("{}");
        }

        [TestMethod]
        public void Create_MissingOrMalformedRulesFile_ThrowsException()
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
                TemplateAnalyzer.Create();

                // Test failed - move file back
                File.Move(movedFile, rulesFile);
                Assert.Fail("Create() method did not throw exception when Rules file is missing.");
            }
            catch (Exception e)
            {
                if (typeof(TemplateAnalyzerException) != e.GetType())
                {
                    // Test failed - move file back
                    File.Move(movedFile, rulesFile);
                    Assert.AreEqual(typeof(TemplateAnalyzerException), e.GetType(), "Exception thrown is not of expected type.");
                }
            }

            // Create new empty file
            File.Create(rulesFile).Close();

            try
            {
                TemplateAnalyzer.Create();
                Assert.Fail("Create() method did not throw exception when Rules file is missing.");
            }
            catch (Exception e)
            {
                Assert.AreEqual(typeof(TemplateAnalyzerException), e.GetType(), "Exception thrown is not of expected type.");
            }
            finally
            {
                File.Move(movedFile, rulesFile, overwrite: true);
            }
        }
    }
}