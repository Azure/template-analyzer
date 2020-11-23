// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Armory.Utilities;
using Azure.ResourceManager.Deployments.Core.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace Armory.TemplateProcessor
{
    public class ArmTemplateProcessor
    {
        private string armTemplate;

        public ArmTemplateProcessor(string armTemplate)
        {
            this.armTemplate = armTemplate;
        }

        public void ProcessTemplate()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Populates the deployment metadata data object.
        /// </summary>
        /// <param name="metadata">The deployment metadata <c>JSON</c>.</param>
        /// <returns>A dictionary with the metadata.</returns>
        internal InsensitiveDictionary<JToken> PopulateDeploymentMetadata(string metadata)
        {
            try
            {
                var metadataAsJObject = JObject.Parse(metadata);
                InsensitiveDictionary<JToken> metadataDictionary = new InsensitiveDictionary<JToken>();

                foreach (var property in metadataAsJObject.Properties())
                {
                    metadataDictionary.Add(property.Name, property.Value.ToObject<JToken>());
                }

                return metadataDictionary;
            }
            catch (JsonReaderException ex)
            {
                throw new Exception($"Error parsing metadata: {ex}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error populating metadata: {ex}");
            }
        }

        /// <summary>
        /// Populates the parameters with the parameters.
        /// </summary>
        /// <param name="parameters">The required input parameters and their values <c>JSON</c>.</param>
        /// <returns>A dictionary with required parameters.</returns>
        internal InsensitiveDictionary<JToken> PopulateParameters(string parameters)
        {
            // Create the minimum parameters needed
            JObject parametersObject = JObject.Parse(parameters);
            InsensitiveDictionary<JToken> parametersDictionary = new InsensitiveDictionary<JToken>();

            if (parametersObject["parameters"] == null)
            {
                throw new Exception("Parameteres property is not specified in the ARM Template parameters provided. Please ensure ARM Template parameters follows the following JSON schema https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#");
            }

            foreach (var parameter in parametersObject.InsensitiveToken("parameters").Value<JObject>()?.Properties() ?? Enumerable.Empty<JProperty>())
            {
                JToken parameterValueAsJToken = parameter.Value.ToObject<JObject>().Property("value")?.Value;

                // See if "reference" was specified instead of "value"
                bool isReference = false;
                if (parameterValueAsJToken == null)
                {
                    parameterValueAsJToken = parameter.Value.ToObject<JObject>().Property("reference")?.Value;
                    if (parameterValueAsJToken != null) isReference = true;
                }

                parametersDictionary.Add(parameter.Name, isReference ? $"REF_NOT_AVAIL_{parameter.Name}" : parameterValueAsJToken ?? string.Empty);
            }

            return parametersDictionary;
        }
    }
}
