// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Azure.Deployments.Core.Collections;
using Azure.Deployments.Core.Entities;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.TemplateProcessor
{
    /// <summary>
    /// Generates placeholder values for required inputs to process templates
    /// </summary>
    internal static class PlaceholderInputGenerator
    {
        /// <summary>
        /// Generates placeholder parameters when no default value is specified in the ARM Template.
        /// </summary>
        /// <param name="armTemplate">The ARM Template to generate parameters for <c>JSON</c>.</param>
        /// <returns>The Json string of the placeholder parameter values.</returns>
        internal static string GeneratePlaceholderParameters(string armTemplate)
        {
            JObject jsonTemplate = JObject.Parse(armTemplate);

            JObject jsonParameters = new JObject();

            JToken parameters = jsonTemplate.InsensitiveToken("parameters");
            if (parameters != null)
            {
                int count = 0;

                foreach (JProperty parameter in parameters.Children<JProperty>())
                {
                    JToken parameterValue = parameter.Value;
                    if (parameterValue.InsensitiveToken("defaultValue") == null)
                    {
                        JToken allowedValues = parameterValue.InsensitiveToken("allowedValues");
                        if (allowedValues != null)
                        {
                            JToken firstAllowedValue = allowedValues.First;

                            if (firstAllowedValue != null)
                            {
                                jsonParameters[parameter.Name] = new JObject(new JProperty("value", firstAllowedValue));
                                continue;
                            }
                        }

                        string parameterTypeString = parameterValue.InsensitiveToken("type")?.Value<string>();
                        if (Enum.TryParse<TemplateParameterType>(parameterTypeString, ignoreCase: true, out var parameterType))
                        {
                            switch (parameterType)
                            {
                                case TemplateParameterType.String:
                                case TemplateParameterType.SecureString:
                                    string stringValue = "defaultString";
                                    int countLength = count.ToString().Length;
                                    int? minLength = parameterValue.InsensitiveToken("minLength")?.Value<int>();
                                    int? maxLength = parameterValue.InsensitiveToken("maxLength")?.Value<int>();
                                    if (minLength.HasValue && stringValue.Length + countLength < minLength)
                                    {
                                        stringValue += new string('a', minLength.Value - stringValue.Length - countLength);
                                    }
                                    else if (maxLength.HasValue && stringValue.Length + countLength > maxLength)
                                    {
                                        stringValue = stringValue[0..(maxLength.Value - countLength)];
                                    }
                                    stringValue += count.ToString();
                                    jsonParameters[parameter.Name] = new JObject(new JProperty("value", stringValue));
                                    break;
                                case TemplateParameterType.Int:
                                    jsonParameters[parameter.Name] = new JObject(new JProperty("value", 1));
                                    break;
                                case TemplateParameterType.Bool:
                                    jsonParameters[parameter.Name] = new JObject(new JProperty("value", true));
                                    break;
                                case TemplateParameterType.Array:
                                    jsonParameters[parameter.Name] = JObject.FromObject(new { value = new[] { "item1", "item2" } });
                                    break;
                                case TemplateParameterType.Object:
                                case TemplateParameterType.SecureObject:
                                    jsonParameters[parameter.Name] = JObject.FromObject(new { value = new { property1 = "value1" } });
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    count++;
                }
            }

            return JObject.FromObject(new { parameters = jsonParameters }).ToString();
        }

        /// <summary>
        /// Returns the deployment metadata with placeholder data. 
        /// Use this if you do not rely on the deployment metadata.
        /// </summary>
        /// <returns>A dictionary with mock metadata.</returns>
        internal static InsensitiveDictionary<JToken> GeneratePlaceholderDeploymentMetadata()
        {
            var deployment = JObject.Parse(@"
            {
                ""name"": ""deploymentname"",
                ""type"": ""deploymenttype"",
                ""location"": ""westus2"",
                ""id"": ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/resourceGroupName"",
                ""properties"": {
                    ""templateLink"": {
                        ""uri"": ""https://deploymenturi"",
                        ""contentVersion"": ""0.0"",
                        ""metadata"": {
                            ""metadata"": ""deploymentmetadata""
                        }
                    }
                }
            }");

            var providers = new JArray
            {
                new JObject
                {
                    new JProperty("namespace", "Microsoft.TestNamespace"),
                    new JProperty("testProperty", "testValue")
                }
            };

            var environment = JObject.Parse(@"{
                    ""name"": ""AzureCloud"",
                    ""gallery"": ""https://gallery.azure.com/"",
                    ""graph"": ""https://graph.windows.net/"",
                    ""portal"": ""https://portal.azure.com"",
                    ""graphAudience"": ""https://graph.windows.net/"",
                    ""activeDirectoryDataLake"": ""https://datalake.azure.net/"",
                    ""batch"": ""https://batch.core.windows.net/"",
                    ""media"": ""https://rest.media.azure.net"",
                    ""sqlManagement"": ""https://management.core.windows.net:8443/"",
                    ""vmImageAliasDoc"": ""https://raw.githubusercontent.com/Azure/azure-rest-api-specs/master/arm-compute/quickstart-templates/aliases.json"",
                    ""resourceManager"": ""https://management.azure.com/"",
                    ""authentication"": {
                        ""loginEndpoint"": ""https://login.windows.net/"",
                        ""audiences"": [
                            ""https://management.core.windows.net/"",
                            ""https://management.azure.com/""
                        ],
                        ""tenant"": ""common"",
                        ""identityProvider"": ""AAD""
                    },
                    ""suffixes"": {
                        ""acrLoginServer"": "".azurecr.io"",
                        ""azureDatalakeAnalyticsCatalogAndJob"": ""azuredatalakeanalytics.net"",
                        ""azureDatalakeStoreFileSystem"": ""azuredatalakestore.net"",
                        ""keyvaultDns"": "".vault.azure.net"",
                        ""sqlServerHostname"": "".database.windows.net"",
                        ""storage"": ""core.windows.net""
                    }                    
                }");

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
                { "deployment", deployment },
                { "tenantId", "00000000-0000-0000-0000-000000000000" },
                { "providers", providers },
                { "environment", environment }
            };

            return metadata;
        }
    }
}