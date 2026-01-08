// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Azure.Deployments.Core.Entities;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.TemplateProcessor
{
    /// <summary>
    /// Generates placeholder values for required inputs to process templates
    /// </summary>
    internal static class PlaceholderInputGenerator
    {
        /// <summary>
        /// azd parameter replacement regex pattern, e.g. ${ENV_VARIABLE} or ${ENV_VARIABLE=defaultValue}
        /// </summary>
        private static readonly Regex AzdParameterReplacementRegex = new Regex(
            @"^\$\{\s*[A-Za-z_][A-Za-z0-9_]*\s*(?:=\s*(?<value>[^}]*))?\s*\}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Generates placeholder parameters for those with no default value specified in the ARM Template and are not specified (or need further processing) in provided values.
        /// </summary>
        /// <param name="armTemplate">The ARM Template to generate parameters for <c>JSON</c>.</param>
        /// <param name="providedParameterValues">Parameter values provided from parameters file.</param>
        /// <returns>The Json string of the placeholder parameter values.</returns>
        internal static string GeneratePlaceholderParameters(string armTemplate, string providedParameterValues = null)
        {
            JObject jsonParameters = new JObject();

            if (providedParameterValues != null)
            {
                JObject definedParameterJson = JObject.Parse(providedParameterValues);
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
            JToken definedTemplateParameters = null;

            var reader = new JsonTextReader(new StringReader(armTemplate));
            while (reader.Read())
            {
                if (reader.Depth == 1 && reader.TokenType == JsonToken.PropertyName)
                {
                    if (string.Equals((string)reader.Value, "parameters", StringComparison.OrdinalIgnoreCase))
                    {
                        reader.Read(); // read StartObjectToken
                        definedTemplateParameters = JToken.ReadFrom(reader);
                        break;
                    }
                }
            }

            if (definedTemplateParameters != null)
            {
                int stringParamCount = 0;

                foreach (JProperty templateParameterDefinition in definedTemplateParameters.Children<JProperty>())
                {
                    JToken parameterProperties = templateParameterDefinition.Value;

                    JToken parameterDefaultValue = parameterProperties.InsensitiveToken("defaultValue");
                    bool parameterIsSpecified = jsonParameters.ContainsKey(templateParameterDefinition.Name);
                    bool requiredParamIsMissing = parameterDefaultValue == null && !parameterIsSpecified;
                    JToken parameterValue = parameterIsSpecified ? jsonParameters[templateParameterDefinition.Name]?.InsensitiveToken("value") : null;

                    // Check if the parameter value is an azd CLI placeholder that needs substitution
                    // See https://learn.microsoft.com/azure/developer/azure-developer-cli/manage-environment-variables#use-environment-variables-in-infrastructure-files
                    bool parameterValueIsPlaceholder = false;
                    string parameterSubstitution = null;
                    if (parameterIsSpecified && parameterValue?.Type == JTokenType.String)
                    {
                        var match = AzdParameterReplacementRegex.Match(parameterValue.Value<string>());
                        if (match.Success)
                        {
                            parameterValueIsPlaceholder = true;
                            parameterSubstitution = match.Groups.TryGetValue("value", out var group) && group.Success ? group.Value : null;
                        }
                    }

                    if (requiredParamIsMissing || parameterValueIsPlaceholder)
                    {
                        string parameterTypeString = parameterProperties.InsensitiveToken("type")?.Value<string>();
                        JToken allowedValues = parameterProperties.InsensitiveToken("allowedValues");

                        if (Enum.TryParse<TemplateParameterType>(parameterTypeString, ignoreCase: true, out var parameterType))
                        {
                            if (parameterSubstitution != null)
                            {
                                var valueToUse = ParseParameterValue(parameterType, parameterSubstitution);
                                if (valueToUse != null)
                                {
                                    jsonParameters[templateParameterDefinition.Name] = new JObject(new JProperty("value", valueToUse));
                                    continue;
                                }
                            }

                            if (allowedValues != null)
                            {
                                JToken firstAllowedValue = allowedValues.First;

                                if (firstAllowedValue != null)
                                {
                                    jsonParameters[templateParameterDefinition.Name] = new JObject(new JProperty("value", firstAllowedValue));
                                    continue;
                                }
                            }

                            switch (parameterType)
                            {
                                case TemplateParameterType.String:
                                case TemplateParameterType.SecureString:
                                    string stringValue = "defaultString";
                                    int countLength = stringParamCount.ToString().Length;
                                    int? minLength = parameterProperties.InsensitiveToken("minLength")?.Value<int>();
                                    int? maxLength = parameterProperties.InsensitiveToken("maxLength")?.Value<int>();
                                    if (minLength.HasValue && stringValue.Length + countLength < minLength)
                                    {
                                        stringValue += new string('a', minLength.Value - stringValue.Length - countLength);
                                    }
                                    else if (maxLength.HasValue && stringValue.Length + countLength > maxLength)
                                    {
                                        stringValue = stringValue[0..(maxLength.Value - countLength)];
                                    }
                                    stringValue += (stringParamCount++).ToString();
                                    jsonParameters[templateParameterDefinition.Name] = new JObject(new JProperty("value", stringValue));
                                    break;
                                case TemplateParameterType.Int:
                                    jsonParameters[templateParameterDefinition.Name] = new JObject(new JProperty("value", 1));
                                    break;
                                case TemplateParameterType.Bool:
                                    jsonParameters[templateParameterDefinition.Name] = new JObject(new JProperty("value", true));
                                    break;
                                case TemplateParameterType.Array:
                                    jsonParameters[templateParameterDefinition.Name] = JObject.FromObject(new { value = new[] { "item1", "item2" } });
                                    break;
                                case TemplateParameterType.Object:
                                case TemplateParameterType.SecureObject:
                                    jsonParameters[templateParameterDefinition.Name] = JObject.FromObject(new { value = new { property1 = "value1" } });
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }

            return JObject.FromObject(new { parameters = jsonParameters }).ToString();
        }

        private static JValue ParseParameterValue(TemplateParameterType type, string value) =>
            type switch
            {
                TemplateParameterType.Bool =>
                    new JValue(bool.TryParse(value, out bool parsed) ? parsed : false),
                TemplateParameterType.Int =>
                    new JValue(int.TryParse(value, out int parsed) ? parsed : 0),
                TemplateParameterType.String or TemplateParameterType.SecureString =>
                    new JValue(value),
                _ => null
            };

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