// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Armory.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Armory.Core.UnitTests
{
    [TestClass]
    public class ArmoryTests
    {
        [DataTestMethod]
        [DataRow(@"{ ""azureActiveDirectory"": { ""tenantId"": ""tenantId"" } }", 1, true, DisplayName = "Matching Resource with one passing result")]
        [DataRow(@"{ ""azureActiveDirectory"": { ""someProperty"": ""propertyValue"" } }", 1, false, DisplayName = "Matching Resource with one failing result")]
        [DataRow(@"{ ""property1"": { ""someProperty"": ""propertyValue"" } }", 0, false, DisplayName = "0 matching Resources with no results")]
        public void EvaluateRulesAgainstTemplate_ValidInputValues_ReturnCorrectResults(string resourceProperties, int expectedResultCount, bool expectedResult)
        {
            string template = GenerateTemplate(resourceProperties, expectedResultCount);

            Armory armory = new Armory(template);
            var results = armory.EvaluateRulesAgainstTemplate();

            Assert.AreEqual(expectedResultCount, results.Count());

            if (expectedResultCount > 0)
                Assert.AreEqual(expectedResult, results.First().Passed);
        }

        private string GenerateTemplate(string resourceProperties, int expectedResultCount)
        {
            string resourceType = expectedResultCount > 0 ? "Microsoft.ServiceFabric/clusters" : "Microsoft.Storage/storageAccounts";

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
            new Armory(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArmoryException))]
        public void EvaluateRulesAgainstTemplate_TemplateIsInvalid_ThrowArmoryException()
        {
            Armory armory = new Armory("{}");
            armory.EvaluateRulesAgainstTemplate();
        }
    }
}
