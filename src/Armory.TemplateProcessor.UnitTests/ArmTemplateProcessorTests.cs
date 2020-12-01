// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Armory.Utilities;
using Azure.Deployments.Core.Collections;
using Azure.Deployments.Core.Extensions;
using Azure.Deployments.Templates.Engines;
using Azure.Deployments.Templates.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Armory.TemplateProcessor.UnitTests
{
    [TestClass]
    public class ArmTemplateProcessorTests
    {
        private static ArmTemplateProcessor _armTemplateProcessor;

        [ClassInitialize]
        public static void ClassInit(TestContext testContext)
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
        }

        [TestMethod]
        public void PopulateMetadata_ValidJsonAsInput_ReturnMetadataDictionary()
        {
            string metadata = @"{
                ""subscription"": {
                    ""id"": ""/subscriptions/00000000-0000-0000-0000-000000000000"",
                    ""subscriptionId"": ""/subscriptions/00000000-0000-0000-0000-000000000000"",
                    ""tenantId"": ""00000000-0000-0000-0000-000000000001""
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
                ""tenantId"": ""00000000-0000-0000-0000-000000000001""
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
        public void ProcessTemplateResourceAndOutputPropertiesLanguageExpressions_ValidTemplateWithExpressionInResourceProperties_ProcessResourceProperyLanguageExpressions()
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

            Template template = TemplateEngine.ParseTemplate(templateJson);
            _armTemplateProcessor.ProcessTemplateResourceAndOutputPropertiesLanguageExpressions(template);

            Assert.IsTrue(template.Resources.First().Properties.Value.InsensitiveToken("dnsSettings.domainNameLabel").Value<string>().StartsWith("linux-vm-"));
        }

        [DataTestMethod]
        [DataRow(@"[reference(variables('publicIPAddressName')).dnsSettings.fqdn]", "NOT_PARSED", DisplayName = "Has reference expression. Returns NOT_PARSED.")]
        [DataRow(@"[concat('output-', uniqueString('uniquestring'))]", "output-", DisplayName = "Has language expression. Returns evaluated expression.")]
        public void ProcessTemplateResourceAndOutputPropertiesLanguageExpressions_ValidTemplateWithReferenceExpressionInOutputs_ProcessOutputValueLanguageExpressions(string outputValue, string expectedValue)
        {
            string templateJson = GenerateTemplate(outputValue);

            Template template = TemplateEngine.ParseTemplate(templateJson);
            _armTemplateProcessor.ProcessTemplateResourceAndOutputPropertiesLanguageExpressions(template);

            Assert.IsTrue(template.Outputs["output0"].Value.Value.ToString().StartsWith(expectedValue));
        }

        private string GenerateTemplate(string outputValue)
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

        [TestMethod]
        public void ParseAndValidateTemplate_ValidTemplateWithExpressionInResourceProperties_ProcessResourceProperyLanguageExpressions()
        {
            var parameters = new InsensitiveDictionary<JToken>();

            var metadata = new InsensitiveDictionary<JToken>
            {
                { "subscription", new JObject(
                    new JProperty("id", "/subscriptions/00000000-0000-0000-0000-000000000000"),
                    new JProperty("subscriptionId", "00000000-0000-0000-0000-000000000000"),
                    new JProperty("tenantId", "00000000-0000-0000-0000-000000000000")) },
                { "resourceGroup", new JObject(
                    new JProperty("id", "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/resourceGroupName"),
                    new JProperty("location", "westus2"),
                    new JProperty("name", "resource-group")) },
                { "tenantId", "00000000-0000-0000-0000-000000000000" }
            };

            Template template = _armTemplateProcessor.ParseAndValidateTemplate(parameters, metadata);

            Assert.IsTrue(template.Resources.First().Properties.Value.InsensitiveToken("dnsSettings.domainNameLabel").Value<string>().StartsWith("linux-vm-"));
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

            var parameters = new InsensitiveDictionary<JToken>();

            var metadata = new InsensitiveDictionary<JToken>
            {
                { "subscription", new JObject(
                    new JProperty("id", "/subscriptions/00000000-0000-0000-0000-000000000000"),
                    new JProperty("subscriptionId", "00000000-0000-0000-0000-000000000000"),
                    new JProperty("tenantId", "00000000-0000-0000-0000-000000000000")) },
                { "resourceGroup", new JObject(
                    new JProperty("id", "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/resourceGroupName"),
                    new JProperty("location", "westus2"),
                    new JProperty("name", "resource-group")) },
                { "tenantId", "00000000-0000-0000-0000-000000000000" }
            };

            Template template = armTemplateProcessor.ParseAndValidateTemplate(parameters, metadata);

            Assert.AreEqual(2, template.Resources.Length);

            Assert.AreEqual("name/SSH-VMNamePrefix-0", template.Resources.First().Name.Value);
            Assert.AreEqual("[concat('name', '/', 'SSH-', 'VMNamePrefix-', copyIndex())]", template.Resources.First().OriginalName);
            Assert.AreEqual(2200, template.Resources.First().Properties.Value.InsensitiveToken("frontendPort").Value<int>());

            Assert.AreEqual("name/SSH-VMNamePrefix-1", template.Resources.Last().Name.Value);
            Assert.AreEqual("[concat('name', '/', 'SSH-', 'VMNamePrefix-', copyIndex())]", template.Resources.Last().OriginalName);
            Assert.AreEqual(2201, template.Resources.Last().Properties.Value.InsensitiveToken("frontendPort").Value<int>());
        }
    }
}
