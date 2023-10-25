// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Azure.Deployments.Core.Entities;
using Newtonsoft.Json.Linq;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json;
using System.IO;

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
        /// <param name="definedParameters">Parameters with defined values to use</param>
        /// <returns>The Json string of the placeholder parameter values.</returns>
        internal static string GeneratePlaceholderParameters(string armTemplate, string definedParameters = null)
        {
            JObject jsonParameters = new JObject();

            if (definedParameters != null)
            {
                JObject definedParameterJson = JObject.Parse(definedParameters);
                if (definedParameterJson.InsensitiveToken("parameters") is JObject paramsObject)
                {
                    jsonParameters = paramsObject;
                }
                else
                {
                    throw new Exception("Parameters property is not specified in the ARM Template parameters provided. Please ensure ARM Template parameters follows the following JSON schema https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#");
                }
            }

            // parse only parameter data from the template to avoid additional overhead
            JToken parameters = null;

            var reader = new JsonTextReader(new StringReader(armTemplate));
            while (reader.Read())
            {
                if (reader.Depth == 1 && reader.TokenType == JsonToken.PropertyName)
                {
                    if (string.Equals((string)reader.Value, "parameters", StringComparison.OrdinalIgnoreCase))
                    {
                        reader.Read(); // read StartObjectToken
                        parameters = JToken.ReadFrom(reader);
                        break;
                    }
                }
            }

            if (parameters != null)
            {
                int count = 0;

                foreach (JProperty parameter in parameters.Children<JProperty>())
                {
                    JToken parameterValue = parameter.Value;
                    if (!jsonParameters.ContainsKey(parameter.Name) && parameterValue.InsensitiveToken("defaultValue") == null)
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
                ""name"": ""placeholderDeploymentName"",
                ""type"": ""placeholderDeploymentType"",
                ""location"": ""westus2"",
                ""id"": ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/placeholderResourceGroup"",
                ""properties"": {
                    ""templateLink"": {
                        ""uri"": ""https://deploymenturi"",
                        ""contentVersion"": ""0.0"",
                        ""metadata"": {
                            ""metadata"": ""placeholderDeploymentMetadata""
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

            var environment = JObject.Parse(@"
            {
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

            var managementGroup = JObject.Parse(@"
            {
                ""id"": ""/providers/Microsoft.Management/managementGroups/placeholderManagementGroup"",
                ""name"": ""placeholderManagementGroup"",
                ""properties"": {
                  ""details"": {
                    ""parent"": {
                      ""displayName"": ""Placeholder Tenant Root Group"",
                      ""id"": ""/providers/Microsoft.Management/managementGroups/00000000-0000-0000-0000-000000000000"",
                      ""name"": ""00000000-0000-0000-0000-000000000000""
                    },
                    ""updatedBy"": ""00000000-0000-0000-0000-000000000000"",
                    ""updatedTime"": ""2020-07-23T21:05:52.661306Z"",
                    ""version"": ""1""
                  },
                  ""displayName"": ""Placeholder Management Group"",
                  ""tenantId"": ""00000000-0000-0000-0000-000000000000""
                },
                ""type"": ""/providers/Microsoft.Management/managementGroups""
            }");

            var subscription = JObject.Parse(@"
            {
                ""id"": ""/subscriptions/00000000-0000-0000-0000-000000000000"",
                ""subscriptionId"": ""00000000-0000-0000-0000-000000000000"",
                ""tenantId"": ""00000000-0000-0000-0000-000000000000"",
                ""displayName"": ""Placeholder Subscription Name""
            }");

            var resourceGroup = JObject.Parse(@"
            {
                ""id"": ""/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/placeholderResourceGroup"",
                ""name"": ""placeholderResourceGroup"",
                ""type"":""Microsoft.Resources/resourceGroups"",
                ""location"": ""westus2"",
                ""properties"": {
                    ""provisioningState"": ""Succeeded""
                }
            }");

            var tenant = JObject.Parse(@"
            {
                ""countryCode"": ""US"",
                ""displayName"": ""Placeholder Tenant"",
                ""id"": ""/tenants/00000000-0000-0000-0000-000000000000"",
                ""tenantId"": ""00000000-0000-0000-0000-000000000000""
            }");

            var metadata = new InsensitiveDictionary<JToken>
            {
                { "subscription", subscription },
                { "resourceGroup", resourceGroup },
                { "managementGroup", managementGroup },
                { "deployment", deployment },
                { "tenantId", "00000000-0000-0000-0000-000000000000" },
                { "tenant", tenant },
                { "providers", providers },
                { "environment", environment }
            };

            return metadata;
        }
    }
}