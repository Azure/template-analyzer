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
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", "Microsoft.ServiceFabric/clusters", 1, true, DisplayName = "Matching Resource with one passing evaluation")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.ServiceFabric/clusters", 1, false, DisplayName = "Matching Resource with one failing evaluation")]
        [DataRow(@"{ ""property1"": { ""someProperty"": ""propertyValue"" } }", "Microsoft.Storage/storageAccounts", 0, false, DisplayName = "0 matching Resources with no results")]
        public void EvaluateRulesAgainstTemplate_ValidInputValues_ReturnCorrectEvaluations(string resourceProperties, string resourceType, int expectedEvaluationCount, bool expectedEvaluationPassed)
        {
            string template = GenerateTemplate(resourceProperties, resourceType);

            TemplateAnalyzer templateAnalyzer = new TemplateAnalyzer(template);
            var evaluations = templateAnalyzer.EvaluateRulesAgainstTemplate();

            Assert.AreEqual(expectedEvaluationCount, evaluations.Count());

            if (expectedEvaluationCount > 0)
                Assert.AreEqual(expectedEvaluationPassed, evaluations.First().Passed);
        }

        private string GenerateTemplate(string resourceProperties, string resourceType)
        {
            return string.Format(@"{{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""resources"": [
                    {{
                      ""apiVersion"": ""2018-02-01"",
                      ""name"": ""resourceName"",
                      ""type"": ""{1}"",
                      ""properties"": {0}
                    }}
            ]}}", resourceProperties, resourceType);
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
