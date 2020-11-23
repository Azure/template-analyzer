// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.ResourceManager.Deployments.Core.Collections;
using Azure.ResourceManager.Deployments.Core.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;

namespace Armory.TemplateProcessor.UnitTests
{
    [TestClass]
    public class ArmTemplateProcessorTests
    {
        private static ArmTemplateProcessor _armTemplateProcessor;

        [ClassInitialize]
        public static void ClassInit(TestContext testContext)
        {
            _armTemplateProcessor = new ArmTemplateProcessor("{}");
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
    }
}
