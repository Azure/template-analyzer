// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Armory.Core.UnitTests
{
    [TestClass]
    public class RunnerTests
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
        public void Run_ValidInputValues_ReturnCorrectResults(string template, int expectedResultCount, bool expectedResult)
        {
            var results = Runner.Run(template, null);

            Assert.AreEqual(expectedResultCount, results.Count());

            if (expectedResultCount > 0)
                Assert.AreEqual(expectedResult, results.First().Passed);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Run_TemplateIsNull_ThrowArgumentNullException()
        {
            Runner.Run(null, null);
        }
    }
}
