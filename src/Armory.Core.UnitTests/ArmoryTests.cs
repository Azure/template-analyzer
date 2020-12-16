// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Armory.Core.UnitTests
{
    [TestClass]
    public class ArmoryTests
    {
        [DataTestMethod]
        [DataRow(@"{
                  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                  ""contentVersion"": ""1.0.0.0"",
                  ""parameters"": {
                  },
                  ""variables"": {
                  },
                  ""resources"": [
                    {
                      ""name"": ""resourceName"",
                      ""type"": ""Microsoft.ServiceFabric/clusters"",
                      ""properties"": {
                        ""azureActiveDirectory"": {
                          ""tenantId"": ""tenantId""
                        }
                      }
                    }
                  ],
                  ""outputs"": {
                  }
                }
                ", 1, true, DisplayName = "Matching Resource with one passing result")]
        [DataRow(@"{
                  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                  ""contentVersion"": ""1.0.0.0"",
                  ""parameters"": {
                  },
                  ""variables"": {
                  },
                  ""resources"": [
                    {
                      ""name"": ""resourceName"",
                      ""type"": ""Microsoft.ServiceFabric/clusters"",
                      ""properties"": {
                        ""azureActiveDirectory"": {
                          ""someProperty"": ""propertyValue""
                        }
                      }
                    }
                  ],
                  ""outputs"": {
                  }
                }
                ", 1, false, DisplayName = "Matching Resource with one failing result")]
        [DataRow(@"{
                  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                  ""contentVersion"": ""1.0.0.0"",
                  ""parameters"": {
                  },
                  ""variables"": {
                  },
                  ""resources"": [
                    {
                      ""name"": ""resourceName"",
                      ""type"": ""Microsoft.ResoureProvider/resource"",
                      ""properties"": {
                        ""property1"": {
                          ""someProperty"": ""propertyValue""
                        }
                      }
                    }
                  ],
                  ""outputs"": {
                  }
                }
                ", 0, false, DisplayName = "0 matching Resources with no results")]
        public void EvaluateRulesAgainstTemplate_ValidInputValues_ReturnCorrectResults(string template, int expectedResultCount, bool expectedResult)
        {
            var results = Armory.EvaluateRulesAgainstTemplate(template, null);

            Assert.AreEqual(expectedResultCount, results.Count());

            if (expectedResultCount > 0)
                Assert.AreEqual(expectedResult, results.First().Passed);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EvaluateRulesAgainstTemplate_TemplateIsNull_ThrowArgumentNullException()
        {
            Armory.EvaluateRulesAgainstTemplate(null, null);
        }
    }
}
