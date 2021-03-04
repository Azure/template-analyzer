// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.Core.UnitTests
{
    [TestClass]
    public class TemplateAnalyzerTests
    {
        [DataTestMethod]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, true, DisplayName = "1 matching Resource with 1 passing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.ServiceFabric/clusters", 1, false, DisplayName = "1 matching Resource with 1 failing evaluation")]
        [DataRow(@"{ ""property1"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.Storage/storageAccounts", 0, false, DisplayName = "0 matching Resources with no results")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, false, @"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", DisplayName = "2 matching Resources with 1 passing evaluation")]
        public void EvaluateRulesAgainstTemplate_ValidInputValues_ReturnCorrectEvaluations(string resource1Properties, string resourceType, int expectedEvaluationCount, bool expectedEvaluationPassed, string resource2Properties = null)
        {
            // Arrange
            string[] resourceProperties = { GenerateResource(resource1Properties, resourceType), GenerateResource(resource2Properties, resourceType) };
            string template = GenerateTemplate(resourceProperties);

            TemplateAnalyzer templateAnalyzer = new TemplateAnalyzer(template);
            var evaluations = templateAnalyzer.EvaluateRulesAgainstTemplate();

            Assert.AreEqual(expectedEvaluationCount, evaluations.Count());

            if (expectedEvaluationCount > 0)
                Assert.AreEqual(expectedEvaluationPassed, evaluations.First().Passed);
        }

        private string GenerateTemplate(string[] resourceProperties)
        {
            return string.Format(@"{{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""resources"": [ {0} ] }}", string.Join(',', resourceProperties));
        }

        private string GenerateResource(string resourceProperties, string resourceType)
        {
            if (string.IsNullOrEmpty(resourceProperties))
            {
                return null;
            }

            return string.Format(@"
                    {{
                      ""apiVersion"": ""2018-02-01"",
                      ""name"": ""resourceName"",
                      ""type"": ""{1}"",
                      ""properties"": {0}
                    }}", resourceProperties, resourceType);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EvaluateRulesAgainstTemplate_TemplateIsNull_ThrowArgumentNullException()
        {
            new TemplateAnalyzer(null);
        }

        [TestMethod]
        [ExpectedException(typeof(TemplateAnalyzerException))]
        public void EvaluateRulesAgainstTemplate_TemplateIsInvalid_ThrowTemplateAnalyzerException()
        {
            TemplateAnalyzer templateAnalyzer = new TemplateAnalyzer("{}");
            templateAnalyzer.EvaluateRulesAgainstTemplate();
        }
    }
}
