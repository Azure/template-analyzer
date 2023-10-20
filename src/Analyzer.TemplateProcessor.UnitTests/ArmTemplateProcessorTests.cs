// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azure.Deployments.Core.Definitions.Schema;
using Azure.Deployments.Core.Json;
using Azure.Deployments.Templates.Engines;
using Azure.Deployments.Templates.Exceptions;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Microsoft.WindowsAzure.ResourceStack.Common.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.TemplateProcessor.UnitTests
{
    [TestClass]
    public class ArmTemplateProcessorTests
    {
        private static ArmTemplateProcessor _armTemplateProcessor;
        private static InsensitiveDictionary<JToken> _templateMetadata;
        private static InsensitiveDictionary<JToken> _templateParameters;
        private Func<string, TemplateResource> _getTemplateFromString = templateString => JObject.Parse(templateString).ToObject<TemplateResource>(SerializerSettings.SerializerWithObjectTypeSettings);

        /// <summary>
        /// Test data for tests that verify behavior for copied resources with dependencies
        /// Index 1: Template resouces
        /// Index 2: Expected resource mapping
        /// Index 3: Expected resource names in the final template
        /// Index 4: Array containing an array of internal resource names for each resource in the final template
        /// Index 5: Test display name, this is just so GetDisplayName() can do a lookup, and is not used in the test itself
        /// </summary>
        public static IReadOnlyList<object[]> ScenariosOfCopiesWithDependencies { get; } = new List<object[]>
        {
            new object[] {
                new string[] {
                    @"{
                        ""type"": ""Microsoft.Compute/virtualMachines"",
                        ""name"": ""[concat('vmName', copyIndex())]"",
                        ""apiVersion"": ""2020-12-01"",
                        ""copy"": {
                            ""name"": ""virtualMachineLoop"",
                            ""count"": 2
                        },
                        ""dependsOn"": [
                            ""storageAccountName"",
                            ""asName""
                        ],
                        ""properties"": {
                        }
                    }",
                    @"{
                        ""type"": ""Microsoft.Compute/availabilitySets"",
                        ""name"": ""asName"",
                        ""apiVersion"": ""2020-12-01"",
                        ""properties"": {
                        }
                    }",
                    @"{
                        ""type"": ""Microsoft.Storage/storageAccounts"",
                        ""name"": ""storageAccountName"",
                        ""apiVersion"": ""2020-12-01"",
                        ""properties"": {
                        }
                    }"
                },
                new Dictionary<string, string> {
                    { "resources[0]", "resources[1]" },
                    { "resources[1]", "resources[2]" },
                    { "resources[2]", "resources[0]" },
                    { "resources[3]", "resources[0]" },
                    { "resources[0].resources[0]", "resources[0]" },
                    { "resources[0].resources[1]", "resources[0]" },
                    { "resources[1].resources[0]", "resources[0]" },
                    { "resources[1].resources[1]", "resources[0]" }
                },
                new string[] { "asName", "storageAccountName", "vmName0", "vmName1" },
                new string[][] { new string[] { "vmName0", "vmName1" }, new string[] { "vmName0", "vmName1" }, new string[] { }, new string[] { } },
                "Copied resource that depends on others"
            },
            new object[] {
                new string[] {
                    @"{
                        ""type"": ""Microsoft.Network/networkInterfaces"",
                        ""name"": ""[concat('ni', copyindex())]"",
                        ""apiVersion"": ""2016-03-30"",
                        ""dependsOn"": [
                        ],
                        ""copy"": {
                            ""name"": ""WebTierNicLoop"",
                            ""count"": 2
                        },
                        ""properties"": {
                        }
                    }",
                    @"{
                        ""type"": ""Microsoft.Compute/virtualMachines"",
                        ""name"": ""[concat('vm', copyindex())]"",
                        ""apiVersion"": ""2017-03-30"",
                        ""dependsOn"": [
                            ""[resourceId('Microsoft.Network/networkInterfaces/', concat('ni', copyindex()))]""
                        ],
                        ""copy"": {
                            ""name"": ""WebTierVMLoop"",
                            ""count"": 2
                        },
                        ""properties"": {
                        }
                    }"
                },
                new Dictionary<string, string> {
                    { "resources[0]", "resources[0]" },
                    { "resources[1]", "resources[0]" },
                    { "resources[2]", "resources[1]" },
                    { "resources[3]", "resources[1]" },
                    { "resources[0].resources[0]", "resources[1]" },
                    { "resources[1].resources[0]", "resources[1]" }
                },
                new string[] { "ni0", "ni1", "vm0", "vm1" },
                new string[][] { new string[] { "vm0" }, new string[] { "vm1" }, new string[] { }, new string[] { } },
                "Copied resources where index 0 depends on the other index 0, and index 1 on index 1"
            },
            new object[] {
                new string[] {
                    @"{
                        ""type"": ""Microsoft.Compute/availabilitySets"",
                        ""name"": ""[concat('as', copyindex(1))]"",
                        ""apiVersion"": ""2017-12-01"",
                        ""copy"": {
                            ""name"": ""availSetLoop"",
                            ""count"": 2
                        },
                        ""properties"": {
                        }
                    }",
                    @"{
                        ""type"": ""Microsoft.Compute/virtualMachines"",
                        ""name"": ""vms0"",
                        ""apiVersion"": ""2017-03-30"",
                        ""dependsOn"": [
                            ""as1""
                        ],
                        ""properties"": {
                        }
                    }",
                    @"{
                        ""type"": ""Microsoft.Compute/virtualMachines"",
                        ""name"": ""vms1"",
                        ""apiVersion"": ""2017-03-30"",
                        ""dependsOn"": [
                            ""as2""
                        ],
                        ""properties"": {
                        }
                    }"
                },
                new Dictionary<string, string> {
                    { "resources[0]", "resources[1]" },
                    { "resources[1]", "resources[2]" },
                    { "resources[2]", "resources[0]" },
                    { "resources[3]", "resources[0]" },
                    { "resources[2].resources[0]", "resources[1]" },
                    { "resources[3].resources[0]", "resources[2]" }
                },
                new string[] { "vms0", "vms1", "as1", "as2" },
                new string[][] { new string[] { }, new string[] { }, new string[] { "vms0" }, new string[] { "vms1" } },
                "Copied resource with different resources depending on each copy"
            },
        }.AsReadOnly();

        // Just returns the element in the last index of the array from ScenariosOfCopiesWithDependencies
        public static string GetDisplayName(MethodInfo _, object[] data) => (string)data[^1];

        [TestInitialize]
        public void TestInit()
        {
            _armTemplateProcessor = new ArmTemplateProcessor(@"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""parameters"": { },
                ""variables"": { },
                ""resources"": [
                    {
                        ""apiVersion"": ""2020-05-01"",
                        ""type"": ""Microsoft.Network/publicIPAddresses"",
                        ""name"": ""publicIPAddress"",
                        ""location"": ""westus"",
                        ""tags"": {},
                        ""properties"": {
                            ""publicIPAllocationMethod"": ""Dynamic"",
                            ""dnsSettings"": {
                                ""domainNameLabel"": ""[concat('linux-vm-', uniqueString(resourceGroup().id))]""
                            }
                        }
                    }
                ],
                ""outputs"": {}
            }");

            _templateMetadata = PlaceholderInputGenerator.GeneratePlaceholderDeploymentMetadata();

            _templateParameters = new InsensitiveDictionary<JToken>();
        }

        [TestMethod]
        public void PopulateMetadata_ValidJsonAsInput_ReturnMetadataDictionary()
        {
            string metadata = @"{
                ""subscription"": {
                    ""id"": ""/subscriptions/00000000-0000-0000-0000-000000000000"",
                    ""subscriptionId"": ""/subscriptions/00000000-0000-0000-0000-000000000000"",
                    ""tenantId"": ""00000000-0000-0000-0000-000000000000""
                },
                ""resourceGroup"": {
                    ""id"": ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/metadataTestresourcegroupname"",
                    ""location"": ""westus2"",
                    ""name"": ""metadataTestresourcegroupname""
                },
                ""deployment"": {
                    ""name"": ""deploymentname"",
                    ""type"": ""deploymenttype"",
                    ""location"": ""westus2"",
                    ""id"": ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/metadataTestresourcegroupname"",
                    ""properties"": {
                        ""templateLink"": {
                            ""uri"": ""https://deploymenturi"",
                            ""contentVersion"": ""0.0"",
                            ""metadata"": {
                                ""metadata"": ""deploymentmetadata""
                            }
                        }
                    }
                },
                ""tenantId"": ""00000000-0000-0000-0000-000000000000""
            }";

            InsensitiveDictionary<JToken> metadataObj = _armTemplateProcessor.PopulateDeploymentMetadata(metadata);

            Assert.AreEqual("/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/metadataTestresourcegroupname", metadataObj["resourceGroup"]["id"].Value<string>());
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PopulateMetadata_NonValidJsonAsInput_ExceptionThrown()
        {
            string metadataWithMissingColon = @"{ } }";

            _armTemplateProcessor.PopulateDeploymentMetadata(metadataWithMissingColon);
        }

        [TestMethod]
        public void PopulateParameters_ValidJsonAsInput_ReturnParametersDictionary()
        {
            string parameters = @"{
                ""parameters"": {
                    ""parameter0"": {
                        ""value"": ""parameter0Value""
                    }
                }
            }";

            InsensitiveDictionary<JToken> parametersObj = _armTemplateProcessor.PopulateParameters(parameters);

            Assert.AreEqual("parameter0Value", parametersObj["parameter0"].Value<string>());
        }

        [TestMethod]
        public void PopulateParameters_ParametersPropertyIsEmpty_ReturnEmptyParametersDictionary()
        {
            string parameters = @"{
                ""parameters"": { }
            }";

            InsensitiveDictionary<JToken> parametersObj = _armTemplateProcessor.PopulateParameters(parameters);

            Assert.IsFalse(parametersObj.Any());
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void PopulateParameters_ParametersPropertyMisspelled_ExceptionThrown()
        {
            string parameters = @"{
                ""param"": {
                    ""parameter0"": {
                        ""value"": ""parameter0Value""
                    }
                }
            }";

            _armTemplateProcessor.PopulateParameters(parameters);
        }

        [TestMethod]
        public void PopulateParameters_ParameterWithReference_ValueStartsWithPrefix()
        {
            string parameters = @"{
                ""parameters"": {
                    ""parameter0"": {
                        ""reference"": {
                            ""keyVault"": {
                                ""id"": ""keyVaultID""
                            },
                            ""secretName"": ""testSecretName""
                        }
                    }
                }
            }";

            InsensitiveDictionary<JToken> parametersObj = _armTemplateProcessor.PopulateParameters(parameters);

            Assert.AreEqual("REF_NOT_AVAIL_parameter0", parametersObj["parameter0"].Value<string>());
        }

        [TestMethod]
        public void ProcessResourcesAndOutputs_ValidTemplateWithExpressionInResourceProperties_ProcessResourcePropertyLanguageExpressions()
        {
            string templateJson = @"{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""parameters"": { },
                ""variables"": { },
                ""resources"": [
                    {
                        ""apiVersion"": ""2020-05-01"",
                        ""type"": ""Microsoft.Network/publicIPAddresses"",
                        ""name"": ""publicIPAddress"",
                        ""location"": ""westus"",
                        ""tags"": {},
                        ""properties"": {
                            ""publicIPAllocationMethod"": ""Dynamic"",
                            ""dnsSettings"": {
                                ""domainNameLabel"": ""[concat('linux-vm-', uniqueString('uniquestring'))]""
                            }
                        }
                    }
                ],
                ""outputs"": { }
            }";

            Dictionary<string, string> expectedMapping = new Dictionary<string, string> {
                { "resources[0]", "resources[0]" }
            };

            Template template = TemplateEngine.ParseTemplate(templateJson);
            _armTemplateProcessor.ProcessResourcesAndOutputs(template);

            Assert.IsTrue(template.Resources.First().Properties.Value.InsensitiveToken("dnsSettings.domainNameLabel").Value<string>().StartsWith("linux-vm-"));

            AssertDictionariesAreEqual(expectedMapping, _armTemplateProcessor.ResourceMappings);
        }

        [DataTestMethod]
        [DataRow(@"[reference(variables('publicIPAddressName')).dnsSettings.fqdn]", "NOT_PARSED", DisplayName = "Has reference expression. Returns NOT_PARSED.")]
        [DataRow(@"[concat('output-', uniqueString('uniquestring'))]", "output-", DisplayName = "Has language expression. Returns evaluated expression.")]
        public void ProcessResourcesAndOutputs_ValidTemplateWithReferenceExpressionInOutputs_ProcessOutputValueLanguageExpressions(string outputValue, string expectedValue)
        {
            string templateJson = GenerateTemplateWithOutputs(outputValue);

            Dictionary<string, string> expectedMapping = new Dictionary<string, string>();

            Template template = TemplateEngine.ParseTemplate(templateJson);
            _armTemplateProcessor.ProcessResourcesAndOutputs(template);

            Assert.IsTrue(template.Outputs["output0"].Value.Value.ToString().StartsWith(expectedValue));

            AssertDictionariesAreEqual(expectedMapping, _armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void ParseAndValidateTemplate_ValidTemplateWithExpressionInResourceProperties_ProcessResourceProperyLanguageExpressions()
        {
            Dictionary<string, string> expectedMapping = new Dictionary<string, string> {
                { "resources[0]", "resources[0]" }
            };

            Template template = _armTemplateProcessor.ParseAndValidateTemplate(_templateParameters, _templateMetadata);

            Assert.IsTrue(template.Resources.First().Properties.Value.InsensitiveToken("dnsSettings.domainNameLabel").Value<string>().StartsWith("linux-vm-"));

            AssertDictionariesAreEqual(expectedMapping, _armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void ParseAndValidateTemplate_ValidTemplateWithTwoCopyLoops_ProcessResourceCopies()
        {
            string templateJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""parameters"": { },
                ""variables"": { },
                ""resources"": [
                    {
                        ""apiVersion"": ""2016-03-30"",
                        ""copy"": {
                            ""count"": 2,
                            ""name"": ""masterLbLoopNode""
                        },
                        ""location"": ""westus2"",
                        ""name"": ""[concat('name', '/', 'SSH-', 'VMNamePrefix-', copyIndex())]"",
                        ""properties"": {
                            ""backendPort"": 22,
                            ""enableFloatingIP"": false,
                            ""frontendIPConfiguration"": {
                                ""id"": ""Microsoft.Network/loadBalancers/loadBalancer/frontendIPConfigurations/config""
                            },
                            ""frontendPort"": ""[copyIndex(2200)]"",
                            ""protocol"": ""Tcp""
                        },
                        ""type"": ""Microsoft.Network/loadBalancers/inboundNatRules""
                    },
                    {
                        ""apiVersion"": ""2016-03-30"",
                        ""location"": ""westus2"",
                        ""name"": ""resourceWith/NoCopyLoop"",
                        ""properties"": {
                            ""backendPort"": 22,
                            ""enableFloatingIP"": false,
                            ""frontendIPConfiguration"": {
                                ""id"": ""Microsoft.Network/loadBalancers/loadBalancer/frontendIPConfigurations/config""
                            },
                            ""frontendPort"": ""28"",
                            ""protocol"": ""Tcp""
                        },
                        ""type"": ""Microsoft.Network/loadBalancers/inboundNatRules""
                    },
                    {
                        ""apiVersion"": ""2016-03-30"",
                        ""copy"": {
                            ""count"": 2,
                            ""name"": ""loop2""
                        },
                        ""location"": ""westus2"",
                        ""name"": ""[concat('name', '/', 'CopyLoop2-SSH-', 'VMNamePrefix-', copyIndex())]"",
                        ""properties"": {
                            ""backendPort"": 22,
                            ""enableFloatingIP"": false,
                            ""frontendIPConfiguration"": {
                                ""id"": ""Microsoft.Network/loadBalancers/loadBalancer/frontendIPConfigurations/config""
                            },
                            ""frontendPort"": ""[copyIndex(2200)]"",
                            ""protocol"": ""Tcp""
                        },
                        ""type"": ""Microsoft.Network/loadBalancers/inboundNatRules""
                    }
                ],
                ""outputs"": {}
            }";

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(templateJson);
            
            Dictionary<string, string> expectedMapping = new Dictionary<string, string> {
                { "resources[0]", "resources[1]" },
                { "resources[1]", "resources[0]" },
                { "resources[2]", "resources[0]" },
                { "resources[3]", "resources[2]" },
                { "resources[4]", "resources[2]" },
            };

            Template template = armTemplateProcessor.ParseAndValidateTemplate(_templateParameters, _templateMetadata);

            Assert.AreEqual(expectedMapping.Count, template.Resources.Length);
            AssertDictionariesAreEqual(expectedMapping, armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void ParseAndValidateTemplate_ValidTemplateWithCopyIndex_ProcessResourceCopies()
        {
            string templateJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""parameters"": { },
                ""variables"": { },
                ""resources"": [
                    {
                        ""apiVersion"": ""2016-03-30"",
                        ""copy"": {
                            ""count"": 2,
                            ""name"": ""masterLbLoopNode""
                        },
                        ""location"": ""westus2"",
                        ""name"": ""[concat('name', '/', 'SSH-', 'VMNamePrefix-', copyIndex())]"",
                        ""properties"": {
                            ""backendPort"": 22,
                            ""enableFloatingIP"": false,
                            ""frontendIPConfiguration"": {
                                ""id"": ""Microsoft.Network/loadBalancers/loadBalancer/frontendIPConfigurations/config""
                            },
                            ""frontendPort"": ""[copyIndex(2200)]"",
                            ""protocol"": ""Tcp""
                        },
                        ""type"": ""Microsoft.Network/loadBalancers/inboundNatRules""
                    }
                ],
                ""outputs"": {}
            }";

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(templateJson);
            
            Dictionary<string, string> expectedMapping = new Dictionary<string, string> {
                { "resources[0]", "resources[0]" },
                { "resources[1]", "resources[0]" }
            };

            Template template = armTemplateProcessor.ParseAndValidateTemplate(_templateParameters, _templateMetadata);

            Assert.AreEqual(2, template.Resources.Length);

            Assert.AreEqual("name/SSH-VMNamePrefix-0", template.Resources.First().Name.Value);
            Assert.AreEqual("[concat('name', '/', 'SSH-', 'VMNamePrefix-', copyIndex())]", template.Resources.First().OriginalName);
            Assert.AreEqual(2200, template.Resources.First().Properties.Value.InsensitiveToken("frontendPort").Value<int>());

            Assert.AreEqual("name/SSH-VMNamePrefix-1", template.Resources.Last().Name.Value);
            Assert.AreEqual("[concat('name', '/', 'SSH-', 'VMNamePrefix-', copyIndex())]", template.Resources.Last().OriginalName);
            Assert.AreEqual(2201, template.Resources.Last().Properties.Value.InsensitiveToken("frontendPort").Value<int>());

            AssertDictionariesAreEqual(expectedMapping, armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void ParseAndValidateTemplate_ValidTemplateWithDates_DoesNotParseDates()
        {
            string templateJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""parameters"": { },
                ""variables"": { },
                ""resources"": [
                    {
                        ""type"": ""Microsoft.Consumption/budgets"",
                        ""apiVersion"": ""2019-10-01"",
                        ""name"": ""A name"",
                        ""properties"": {
                            ""startDate"": ""2021-02-28"",
                            ""endDate"": ""2031-02-28T18:17:16Z""
                        }
                    }
                ],
                ""outputs"": {}
            }";

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(templateJson);

            Template template = armTemplateProcessor.ParseAndValidateTemplate(_templateParameters, _templateMetadata);

            Assert.AreEqual(JTokenType.String, template.Resources.First().Properties.Value.InsensitiveToken("startDate").Type);
            Assert.AreEqual(JTokenType.String, template.Resources.First().Properties.Value.InsensitiveToken("endDate").Type);
        }

        [DataTestMethod]
        [DynamicData(nameof(ScenariosOfCopiesWithDependencies), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void ParseAndValidateTemplate_ValidTemplatesWithCopiesAndDependencies_ProcessResourceCopies(
            string[] stringResources, Dictionary<string, string> expectedResourceMapping,
            string[] resourceNames, string[][] internalResources, string _)
        {
            var resources = new TemplateResource[] { };
            foreach (string resourceString in stringResources)
            {
                resources = resources.ConcatArray(new TemplateResource[] { _getTemplateFromString(resourceString) });
            }

            var armTemplateProcessor = new ArmTemplateProcessor(GenerateTemplateWithResources(resources));

            Template template = armTemplateProcessor.ParseAndValidateTemplate(_templateParameters, _templateMetadata);

            Assert.AreEqual(resourceNames.Length, internalResources.Length);
            Assert.AreEqual(resourceNames.Length, template.Resources.Length);

            for (int resourceNumber = 0; resourceNumber < internalResources.Length; resourceNumber++)
            {
                Assert.AreEqual(resourceNames[resourceNumber], template.Resources[resourceNumber].Name.Value);

                if (internalResources[resourceNumber].Length == 0)
                {
                    Assert.AreEqual(null, template.Resources[resourceNumber].Resources);
                } 
                else
                {
                    for (int internalResourceNumber = 0; internalResourceNumber < internalResources[resourceNumber].Length; internalResourceNumber++)
                    {
                        Assert.AreEqual(internalResources[resourceNumber][internalResourceNumber],
                            template.Resources[resourceNumber].Resources[internalResourceNumber].Name.Value);
                    }
                }
            }

            AssertDictionariesAreEqual(expectedResourceMapping, armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void CopyResourceDependants_ValidChildResourceDependsOnByNameAndResourceId_ChildResourceGetsCopiedToParentResources()
        {
            // Arrange
            string childTemplateResourceJson = @"{
                ""type"": ""Microsoft.Network/networkInterfaces"",
                ""apiVersion"": ""2018-10-01"",
                ""name"": ""simpleLinuxVMNetInt"",
                ""location"": ""westus"",
                ""dependsOn"": [
                    ""/subscriptions/subId/resourceGroups/resourceGroup/providers/Microsoft.Network/networkSecurityGroups/SecGroupNet"",
                    ""vNet"",
                    ""parentResource""
                ],
                ""properties"": { }
            }"; 
            string parentTemplateResource1Json = @"{
                ""type"": ""Microsoft.Network/networkSecurityGroups"",
                ""apiVersion"": ""2019-02-01"",
                ""name"": ""SecGroupNet"",
                ""location"": ""westus"",
                ""properties"": { }
            }";
            string parentTemplateResource2Json = @"{
                ""type"": ""Microsoft.Network/virtualNetworks"",
                ""apiVersion"": ""2019-04-01"",
                ""name"": ""vNet"",
                ""location"": ""westus"",
                ""properties"": { }
            }";
            string parentTemplateResource3Json = @"{
                ""type"": ""Microsoft.EventGrid/domains"",
                ""apiVersion"": ""2019-04-01"",
                ""name"": ""parentResource"",
                ""resources"": [
                    {
                        ""type"": ""topics"",
                        ""apiVersion"": ""2019-04-01"",
                        ""name"": ""embeddedChildResource"",
                        ""properties"": { },
                        ""resources"": [
                            {
                                ""type"": ""eventSubscriptions"",
                                ""apiVersion"": ""2019-04-01"",
                                ""name"": ""embeddedGrandChildResource"",
                                ""properties"": { }
                            }
                        ],
                    }
                ],
                ""properties"": { }
            }";

            TemplateResource childTemplateResource = _getTemplateFromString(childTemplateResourceJson);
            TemplateResource parentTemplateResource1 = _getTemplateFromString(parentTemplateResource1Json);
            TemplateResource parentTemplateResource2 = _getTemplateFromString(parentTemplateResource2Json);
            TemplateResource parentTemplateResource3 = _getTemplateFromString(parentTemplateResource3Json);

            Template template = new Template { Resources = new TemplateResource[] { childTemplateResource, parentTemplateResource1, parentTemplateResource2, parentTemplateResource3 } };

            Dictionary<string, string> expectedMapping = new Dictionary<string, string> {
                { "resources[0]", "resources[0]" },
                { "resources[1]", "resources[1]" },
                { "resources[1].resources[0]", "resources[0]" },
                { "resources[2]", "resources[2]" },
                { "resources[2].resources[0]", "resources[0]" },
                { "resources[3]", "resources[3]" },
                { "resources[3].resources[0]", "resources[3].resources[0]" },
                { "resources[3].resources[0].resources[0]", "resources[3].resources[0].resources[0]" },
                { "resources[3].resources[1]", "resources[0]" }
            };

            // Act
            _armTemplateProcessor.ProcessResourcesAndOutputs(template);

            // Assert
            var actualResourceArray = template.ToJToken().InsensitiveToken("resources");

            string expectedResourceArray = $"[ {childTemplateResourceJson}, {parentTemplateResource1Json}, {parentTemplateResource2Json}, {parentTemplateResource3Json} ]";
            var expectedResourceJArray = JArray.Parse(expectedResourceArray);
            (expectedResourceJArray[1] as JObject).Add("resources", new JArray { JObject.Parse(childTemplateResourceJson) });
            (expectedResourceJArray[2] as JObject).Add("resources", new JArray { JObject.Parse(childTemplateResourceJson) });
            (expectedResourceJArray[3].InsensitiveToken("resources") as JArray).Add(JObject.Parse(childTemplateResourceJson));

            // The processed template has the full type value of each resource:
            var childResource = expectedResourceJArray[3]["resources"][0];
            childResource["type"] = "Microsoft.EventGrid/domains/topics";
            childResource["resources"][0]["type"] = "Microsoft.EventGrid/domains/topics/eventSubscriptions";
            Assert.IsTrue(JToken.DeepEquals(expectedResourceJArray, actualResourceArray));

            AssertDictionariesAreEqual(expectedMapping, _armTemplateProcessor.ResourceMappings);
        }

        [DataTestMethod]
        [DataRow(@"{
                ""type"": ""Microsoft.Network/networkInterfaces"",
                ""apiVersion"": ""2018-10-01"",
                ""name"": ""simpleLinuxVMNetInt"",
                ""location"": ""westus"",
                ""dependsOn"": [ ],
                ""properties"": { }
            }", DisplayName = "Depends on is empty")]
        [DataRow(@"{
                ""type"": ""Microsoft.Network/networkInterfaces"",
                ""apiVersion"": ""2018-10-01"",
                ""name"": ""simpleLinuxVMNetInt"",
                ""location"": ""westus"",
                ""dependsOn"": [ ""someResource"" ],
                ""properties"": { }
            }", DisplayName = "Child resource depends on resource outside template scope")]
        [DataRow(@"{
                ""type"": ""Microsoft.Network/networkInterfaces"",
                ""apiVersion"": ""2018-10-01"",
                ""name"": ""simpleLinuxVMNetInt"",
                ""location"": ""westus"",
                ""properties"": { }
            }", DisplayName = "Child resource depends on not specified")]
        public void CopyResourceDependants_ChildDependsOnEmptyOrResourceNotInCurrentTemplate_NoResourcesWereChanged(string templateResource0Json)
        {
            // Arrange
            string templateResource1Json = @"{
                ""type"": ""Microsoft.Network/networkSecurityGroups"",
                ""apiVersion"": ""2019-02-01"",
                ""name"": ""SecGroupNet"",
                ""location"": ""westus"",
                ""properties"": { },
                ""dependsOn"": [ ]
            }";

            Dictionary<string, string> expectedMapping = new Dictionary<string, string> { { "resources[0]", "resources[0]" }, { "resources[1]", "resources[1]" } };

            TemplateResource templateResource0 = _getTemplateFromString(templateResource0Json);
            TemplateResource templateResource1 = _getTemplateFromString(templateResource1Json);

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(GenerateTemplateWithResources(new TemplateResource[] { templateResource0, templateResource1 }));

            // Act
            var template = armTemplateProcessor.ProcessTemplate();

            // Assert
            var actualResourceArray = template.InsensitiveToken("resources");

            JObject expectedResource0 = JObject.Parse(templateResource0Json);
            JObject expectedResource1 = JObject.Parse(templateResource1Json);

            expectedResource0.AddIfNotExists("dependsOn", new JArray());

            var expectedResourceJArray = new JArray { expectedResource0, expectedResource1 };

            Assert.IsTrue(JToken.DeepEquals(expectedResourceJArray, actualResourceArray));

            AssertDictionariesAreEqual(expectedMapping, armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void CopyResourceDependants_DependsOnIsSubResource_CopiesToCorrectResource()
        {
            // Arrange
            string childTemplateResourceJson = @"{
                  ""name"": ""resourceName"",
                  ""type"": ""Microsoft.Logic/workflows"",
                  ""location"": ""westus"",
                  ""apiVersion"": ""2016-06-01"",
                  ""dependsOn"": [
                    ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/resourceGroupName/providers/Microsoft.Web/sites/defaultString1/sourcecontrols/web""
                  ],
                  ""properties"": { }
                }";

            string parentTemplateResourceJson = @"{
                  ""apiVersion"": ""2015-08-01"",
                  ""type"": ""Microsoft.Web/sites"",
                  ""name"": ""defaultString1"",
                  ""location"": ""westus"",
                  ""kind"": ""functionapp"",
                  ""properties"": { },
                  ""dependsOn"": [ ],
                  ""resources"": [
                    {
                      ""apiVersion"": ""2015-08-01"",
                      ""name"": ""web"",
                      ""type"": ""sourcecontrols"",
                      ""properties"": { },
                      ""dependsOn"": [ ],
                    }
                  ]
                }";

            Dictionary<string, string> expectedMapping = new Dictionary<string, string> {
                { "resources[0]", "resources[0]" },
                { "resources[1]", "resources[1]" },
                { "resources[1].resources[0]", "resources[1].resources[0]" },
                { "resources[1].resources[0].resources[0]", "resources[0]" }
            };

            TemplateResource childTemplateResource = _getTemplateFromString(childTemplateResourceJson);
            TemplateResource parentTemplateResource = _getTemplateFromString(parentTemplateResourceJson);

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(GenerateTemplateWithResources(new TemplateResource[] { childTemplateResource, parentTemplateResource }));

            // Act
            var template = armTemplateProcessor.ProcessTemplate();

            // Assert
            string expectedResourceArray = $@"[ {childTemplateResourceJson}, {parentTemplateResourceJson} ]";
            var expectedResourceJArray = JArray.Parse(expectedResourceArray);
            (expectedResourceJArray[1].InsensitiveToken("resources")[0] as JObject).Add("resources", new JArray { JObject.Parse(childTemplateResourceJson) });

            var actualResourceArray = template.ToJToken().InsensitiveToken("resources");

            // The processed template has the full type value of each resource:
            expectedResourceJArray[1]["resources"][0]["type"] = "Microsoft.Web/sites/sourcecontrols";
            Assert.IsTrue(JToken.DeepEquals(expectedResourceJArray, actualResourceArray));

            AssertDictionariesAreEqual(expectedMapping, armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void CopyResourceDependants_DependsOnSpecifiedInNestedChildResource()
        {
            // Arrange
            string resourceJson = @"{
                ""type"": ""Microsoft.Sql/servers"",
                ""apiVersion"": ""2020-02-02-preview"",
                ""name"": ""sqlServerName"",
                ""location"": ""location"",
                ""properties"": {},
                ""resources"": [
                  {
                    ""type"": ""databases"",
                    ""apiVersion"": ""2020-02-02-preview"",
                    ""name"": ""dataWarehouseName"",
                    ""location"": ""location'"",
                    ""kind"": ""v12.0,user,datawarehouse"",
                    ""dependsOn"": [
                      ""sqlServerName""
                    ],
                    ""properties"": {},
                    ""resources"": [
                      {
                        ""type"": ""transparentDataEncryption"",
                        ""apiVersion"": ""2017-03-01-preview"",
                        ""name"": ""current"",
                        ""dependsOn"": [
                          ""dataWarehouseName""
                        ],
                        ""properties"": {}
                      }
                    ]
                  }
                ]
              }";

            Dictionary<string, string> expectedMapping = new Dictionary<string, string> {
                { "resources[0]", "resources[0]" },
                { "resources[0].resources[0]", "resources[0].resources[0]" },
                { "resources[0].resources[0].resources[0]", "resources[0].resources[0].resources[0]" }
            };

            TemplateResource templateResource = _getTemplateFromString(resourceJson);

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(GenerateTemplateWithResources(new TemplateResource[] { templateResource }));

            // Act
            var template = armTemplateProcessor.ProcessTemplate();

            // Assert
            string expectedResourceArray = $@"[ {resourceJson} ]";
            var expectedResourceJArray = JArray.Parse(expectedResourceArray);
            (expectedResourceJArray[0] as JObject).Add("dependsOn", new JArray());

            var actualResourceArray = template.ToJToken().InsensitiveToken("resources");

            // The processed template has the full type value of each resource:
            var childResource = expectedResourceJArray[0]["resources"][0];
            childResource["type"] = "Microsoft.Sql/servers/databases";
            childResource["resources"][0]["type"] = "Microsoft.Sql/servers/databases/transparentDataEncryption";
            Assert.IsTrue(JToken.DeepEquals(expectedResourceJArray, actualResourceArray));

            AssertDictionariesAreEqual(expectedMapping, armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void CopyResourceDependants_DependsOnIsChained_CopiesToCorrectResource()
        {
            // Arrange
            string parentTemplateResourceJson = @"{
                ""type"": ""Microsoft.Network/networkSecurityGroups"",
                ""apiVersion"": ""2019-02-01"",
                ""name"": ""SecGroupNet"",
                ""location"": ""westus"",
                ""properties"": { },
                ""dependsOn"": [ ]
            }";

            string childTemplateResourceJson = @"{
                ""type"": ""Microsoft.Network/networkInterfaces"",
                ""apiVersion"": ""2018-10-01"",
                ""name"": ""NetInt"",
                ""location"": ""westus"",
                ""dependsOn"": [
                    ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/resourceGroupName/providers/Microsoft.Network/networkSecurityGroups/SecGroupNet""
                ],
                ""properties"": { }
            }";

            string grandchildTemplateResourceJson = @"{
                ""type"": ""Microsoft.Compute/virtualMachines"",
                ""apiVersion"": ""2019-03-01"",
                ""name"": ""simpleLinuxVM"",
                ""location"": ""westus"",
                ""dependsOn"": [
                    ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/resourceGroupName/providers/Microsoft.Network/networkInterfaces/NetInt""
                ],
                ""properties"": { }
            }";

            Dictionary<string, string> expectedMapping = new Dictionary<string, string> { 
                { "resources[0]", "resources[0]" }, 
                { "resources[0].resources[0]", "resources[1]" },
                { "resources[0].resources[0].resources[0]", "resources[2]" },
                { "resources[1]", "resources[1]" },
                { "resources[1].resources[0]", "resources[2]" },
                { "resources[2]", "resources[2]" }
            };

            TemplateResource parentTemplateResource = _getTemplateFromString(parentTemplateResourceJson);
            TemplateResource childTemplateResource = _getTemplateFromString(childTemplateResourceJson);
            TemplateResource grandchildTemplateResource = _getTemplateFromString(grandchildTemplateResourceJson);

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(GenerateTemplateWithResources(new TemplateResource[] { parentTemplateResource, childTemplateResource, grandchildTemplateResource }));

            // Act
            var template = armTemplateProcessor.ProcessTemplate();

            // Assert
            var actualResourcesArray = template.ToJToken().InsensitiveToken("resources");
            string expectedResourceArray = $@"[ {parentTemplateResourceJson}, {childTemplateResourceJson}, {grandchildTemplateResourceJson} ]";
            var expectedResourceJArray = JArray.Parse(expectedResourceArray);
            // Add child
            (expectedResourceJArray[0] as JObject).Add("resources", new JArray { JObject.Parse(childTemplateResourceJson) });
            // Add grandchild
            (expectedResourceJArray[0].InsensitiveToken("resources")[0] as JObject).Add("resources", new JArray { JObject.Parse(grandchildTemplateResourceJson) });
            // Add child
            (expectedResourceJArray[1] as JObject).Add("resources", new JArray { JObject.Parse(grandchildTemplateResourceJson) });

            Assert.IsTrue(JToken.DeepEquals(expectedResourceJArray, actualResourcesArray));

            AssertDictionariesAreEqual(expectedMapping, armTemplateProcessor.ResourceMappings);
        }

        [DataTestMethod]
        [DataRow(@"{
                ""type"": ""Microsoft.Resources/deployments"",
                ""apiVersion"": ""2019-10-01"",
                ""name"": ""aDeploymentName"",
                ""properties"": {},
                ""dependsOn"": [
                    ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/aResourceName""
                ]
            }", DisplayName = "Child specifying the default subscription id")]
        [DataRow(@"{
                ""type"": ""Microsoft.Resources/deployments"",
                ""apiVersion"": ""2019-10-01"",
                ""name"": ""aDeploymentName"",
                ""properties"": {},
                ""dependsOn"": [
                    ""[subscriptionResourceId('Microsoft.Resources/resourceGroups', 'aResourceName')]""
                ]
            }", DisplayName = "Child using subscriptionResourceId(...)")]
        public void CopyResourceDependants_DependsOnAResourceGroup(string childResourceJson)
        {
            var parentResourceJson = @"{
                ""type"": ""Microsoft.Resources/resourceGroups"",
                ""apiVersion"": ""2019-10-01"",
                ""name"": ""aResourceName"",
                ""properties"": {}
            }";

            var expectedMapping = new Dictionary<string, string> {
                { "resources[0]", "resources[0]" },
                { "resources[1]", "resources[1]" },
                { "resources[0].resources[0]", "resources[1]" }
            };

            TemplateResource parentResource = _getTemplateFromString(parentResourceJson);
            TemplateResource childResource = _getTemplateFromString(childResourceJson);

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(GenerateTemplateWithResources(new TemplateResource[] { parentResource, childResource }));

            armTemplateProcessor.ProcessTemplate();

            AssertDictionariesAreEqual(expectedMapping, armTemplateProcessor.ResourceMappings);
        }

        [TestMethod]
        public void CopyResourceDependants_DependsOnDoesNotSpecifyResourceGroup_ThrowsExpectedException()
        {
            string templateJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""resources"": [
                    {
                        ""type"": ""Microsoft.Resources/deployments"",
                        ""apiVersion"": ""2019-10-01"",
                        ""name"": ""aDeploymentName"",
                        ""properties"": {},
                        ""dependsOn"": [
                            ""/subscriptions/00000000-0000-0000-0000-000000000000""
                        ]
                    }
                ]
            }";

            var armTemplateProcessor = new ArmTemplateProcessor(templateJson);

            try
            {
                var template = armTemplateProcessor.ProcessTemplate();
                Assert.Fail("No exception was thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Equals("Resource group name was not found on parent resource id: /subscriptions/00000000-0000-0000-0000-000000000000"));
            }
        }

        [TestMethod]
        public void ProcessTemplate_ResourceNameWithIncorrectSegmentLength_ThrowsExpectedException()
        {
            string templateJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""resources"": [
                    {
                        ""apiVersion"": ""2020-12-01"",
                        ""type"": ""Microsoft.Compute/virtualMachines"",
                        ""name"": ""vms"",
                        ""properties"": {
                        },
                        ""resources"": [
                            {
                                ""type"": ""extensions"",
                                ""name"": ""vms/InstallDomainController"",
                                ""apiVersion"": ""2020-12-01"",
                                ""properties"": {
                                }
                            }
                        ]
                    }
                ]
            }";

            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(templateJson);

            try
            {
                var template = armTemplateProcessor.ProcessTemplate();
                Assert.Fail("No exception was thrown");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("incorrect segment lengths"));
            }
        }

        [TestMethod]
        public void ProcessTemplate_ValidTemplateUsingEnvironmentFunction_ProcessTemplateFunction()
        {
            string templateJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""parameters"": {
                    ""privateDbDnsZoneName"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""[concat('privatelink', environment().suffixes.sqlServerHostname)]"",
                        ""metadata"": {
                            ""description"": ""Private DNS zone name for database.""
                        }
                    }
                },
                ""resources"": [
                    {
                        ""type"": ""Microsoft.Network/privateDnsZones"",
                        ""apiVersion"": ""2020-06-01"",
                        ""name"": ""[parameters('privateDbDnsZoneName')]""
                    }
                ]
            }";

            var armTemplateProcessor = new ArmTemplateProcessor(templateJson);

            JToken template = armTemplateProcessor.ProcessTemplate();

            Assert.AreEqual("privatelink.database.windows.net", template["resources"][0]["name"]);
        }

        [TestMethod]
        public void ProcessTemplate_ValidTemplateUsingManagementGroupFunction_ProcessTemplateFunction()
        {
            string templateJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-08-01/managementGroupDeploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""resources"": [
                    {
                        ""type"": ""Microsoft.Management/managementGroups"",
                        ""apiVersion"": ""2020-05-01"",
                        ""scope"": ""/"",
                        ""name"": ""aManagementGroupName"",
                        ""properties"": {
                            ""details"": {
                                ""parent"": {
                                    ""id"": ""[managementGroup().id]""
                                }
                            }
                        }
                    }
                ]
            }";

            var armTemplateProcessor = new ArmTemplateProcessor(templateJson);

            JToken template = armTemplateProcessor.ProcessTemplate();

            Assert.AreEqual("/providers/Microsoft.Management/managementGroups/placeholderManagementGroup", template["resources"][0]["properties"]["details"]["parent"]["id"]);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ProcessTemplate_ValidTemplateWithPartialParameterList_ProcessTemplateFunction(bool generateMissingParameters)
        {
            string parametersJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""parameters"": {
                    ""trafficRoutingMethod"": {
                        ""value"": ""Priority""
                    }
                }
            }";

            string templateJson = @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""parameters"": {
                    ""trafficRoutingMethod"": {
                        ""type"": ""string""
                    },
                    ""location"": {
                        ""type"": ""string""
                    }
                },
                ""resources"": [
                    {
                        ""type"": ""Microsoft.Network/trafficmanagerprofiles"",
                        ""apiVersion"": ""2018-08-01"",
                        ""name"": ""testTrafMan"",
                        ""location"": ""[parameters('location')]"",
                        ""properties"": {
                            ""trafficRoutingMethod"": ""[parameters('trafficRoutingMethod')]""
                        }
                    }
                ]
            }";

            var armTemplateProcessor = new ArmTemplateProcessor(templateJson);

            if (generateMissingParameters)
            {
                JToken template = armTemplateProcessor.ProcessTemplate(parametersJson, null, generateMissingParameters);
                Assert.AreEqual(2, template["parameters"].Count());
                Assert.IsNotNull(template["parameters"]["trafficRoutingMethod"]);
                Assert.AreEqual("Priority", template["parameters"]["trafficRoutingMethod"]["value"]);
                Assert.IsNotNull(template["parameters"]["location"]);
            }
            else
            {
                Assert.ThrowsException<TemplateValidationException>(
                    () => armTemplateProcessor.ProcessTemplate(parametersJson, null, generateMissingParameters));
            }
        }

        private string GenerateTemplateWithOutputs(string outputValue)
        {
            return string.Format(@"{{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""parameters"": {{ }},
                ""variables"": {{ }},
                ""resources"": [ ],
                ""outputs"": {{
                    ""output0"": {{
                        ""type"": ""string"",
                        ""value"": ""{0}""
                    }}
                }}
            }}", outputValue);
        }

        private string GenerateTemplateWithResources(TemplateResource[] resources)
        {
            string resourcesJsonString = "";

            for (int i = 0; i < resources.Length; i++)
            {
                var resource = resources[i];

                if (i > 0)
                {
                    resourcesJsonString += ',';
                }

                resourcesJsonString += resource.ToJson();
            }

            return string.Format(@"{{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""parameters"": {{ }},
                ""variables"": {{ }},
                ""resources"": [ {0} ],
                ""outputs"": {{ }}
            }}", resourcesJsonString);
        }

        private void AssertDictionariesAreEqual(Dictionary<string, string> expectedMapping, Dictionary<string, string> actualMapping)
        {
            Assert.AreEqual(expectedMapping.Count, actualMapping.Count);
            foreach (KeyValuePair<string, string> pair in expectedMapping)
            {
                Assert.IsTrue(actualMapping.ContainsKey(pair.Key));
                Assert.AreEqual(pair.Value, actualMapping[pair.Key]);
            }
        }
    }
}