// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Armory.Utilities;
using Azure.ResourceManager.Deployments.Core.Entities;
using Newtonsoft.Json.Linq;
using System;

namespace Armory.TemplateProcessor
{
    internal class PlaceholderInputGenerator
    {
        /// <summary>
        /// Generates placeholder parameters when no default value is specified in the ARM Template.
        /// </summary>
        /// <param name="armTemplate">The ARM Template to generate parameters for.</param>
        /// <returns>The Json string of the placeholder parameter values.</returns>
        internal static string GenerateParameters(string armTemplate)
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
    }
}
