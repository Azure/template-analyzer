// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Armory.Utilities;
using Azure.Deployments.Core.Collections;
using Azure.Deployments.Core.Extensions;
using Azure.Deployments.Expression.Engines;
using Azure.Deployments.Templates.Configuration;
using Azure.Deployments.Templates.Engines;
using Azure.Deployments.Templates.Expressions;
using Azure.Deployments.Templates.Extensions;
using Azure.Deployments.Templates.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Armory.TemplateProcessor
{
    public class ArmTemplateProcessor
    {
        private readonly string _armTemplate;
        private readonly string _apiVersion;
        private readonly bool _dropResourceCopies;

        public ArmTemplateProcessor(string armTemplate, string apiVersion = "2019-05-01", bool dropResourceCopies = false)
        {
            _armTemplate = armTemplate;
            _apiVersion = apiVersion;
            _dropResourceCopies = dropResourceCopies;
        }

        /// <summary>
        /// Processes the ARM template with placeholder parameters and deployment metadata.
        /// </summary>
        /// <returns>The processed template as a Template object.</returns>
        public Template ProcessTemplate()
        {
            return ProcessTemplate(null, null);
        }

        /// <summary>
        /// Processes the ARM template with provided parameters and placeholder deployment metadata.
        /// </summary>
        /// <param name="parameters">The input parameters and their values <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#</param>
        /// <returns>The processed template as a Template object.</returns>
        public Template ProcessTemplate(string parameters)
        {
            return ProcessTemplate(parameters, null);
        }

        /// <summary>
        /// Processes the ARM template with provided parameters and deployment metadata.
        /// </summary>
        /// <param name="parameters">The input parameters and their values <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#</param>
        /// <param name="metadata">The deployment metadata <c>JSON</c>.</param>
        /// <returns>The processed template as a Template object.</returns>
        public Template ProcessTemplate(string parameters, string metadata)
        {
            InsensitiveDictionary<JToken> parametersDictionary = PopulateParameters(string.IsNullOrEmpty(parameters) ? PlaceholderInputGenerator.GeneratePlaceholderParameters(_armTemplate) : parameters);
            InsensitiveDictionary<JToken> metadataDictionary = string.IsNullOrEmpty(metadata) ? PlaceholderInputGenerator.GeneratePlaceholderDeploymentMetadata() : PopulateDeploymentMetadata(metadata);

            return ParseAndValidateTemplate(parametersDictionary, metadataDictionary);
        }

        internal Template ParseAndValidateTemplate(InsensitiveDictionary<JToken> parameters, InsensitiveDictionary<JToken> metadata)
        {
            Template template = new Template();
            Dictionary<string, (string, int)> copyNameMap = new Dictionary<string, (string, int)>();

            template = TemplateEngine.ParseTemplate(_armTemplate);

            TemplateEngine.ValidateTemplate(template, _apiVersion, TemplateDeploymentScope.NotSpecified);

            TemplateEngine.ParameterizeTemplate(
                inputParameters: parameters,
                template: template,
                metadata: metadata);

            SetOriginalResourceNames(template);

            // If there are resources using copies, the original resource will
            // be removed from template.Resources and the copies will be added instead,
            // to the end of the array. This means that OriginalName will be lost
            // in the resource and will be out of order.
            // To work around this, build a map of copy name to OriginalName and index
            // so OriginalName can be updated and the order fixed after copies are finished
            for (int i = 0; i < template.Resources.Length; i++)
            {
                var resource = template.Resources[i];
                if (resource.Copy != null) copyNameMap[resource.Copy.Name.Value] = (resource.OriginalName, i);
            }

            try
            {
                TemplateEngine.ProcessTemplateLanguageExpressions(template, _apiVersion);
            }
            catch
            {
                // Do not throw if there was an issue with evaluating language expressions
            }

            template.Resources = ReorderResourceCopies(template, copyNameMap);

            TemplateEngine.ValidateProcessedTemplate(template, _apiVersion, TemplateDeploymentScope.NotSpecified);

            template = ProcessTemplateResourceAndOutputPropertiesLanguageExpressions(template);

            return template;
        }

        /// <summary>
        /// Processes language expressions in the properties property of the resources and value property of outputs.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>The parsed template as a Template object.</returns>
        internal Template ProcessTemplateResourceAndOutputPropertiesLanguageExpressions(Template template)
        {
            var evaluationHelper = GetTemplateFunctionEvaluationHelper(template);

            for (int i = 0; i < template.Resources.Length; i++)
            {
                var resource = template.Resources[i];

                bool performedWorkaround = false;
                do
                {
                    performedWorkaround = false;
                    try
                    {
                        if (resource.Properties != null)
                        {
                            evaluationHelper.OnGetCopyContext = () => resource.CopyContext;
                            resource.Properties.Value = ExpressionsEngine.EvaluateLanguageExpressionsRecursive(
                                root: resource.Properties.Value,
                                evaluationContext: evaluationHelper.EvaluationContext);
                        }
                    }
                    catch
                    {
                        // Do not throw if there was an issue with evaluating language expressions
                        continue;
                    }
                } while (performedWorkaround);
            }

            if ((template.Outputs?.Count ?? 0) > 0)
            {
                // Recreate evaluation helper with newly parsed properties
                evaluationHelper = GetTemplateFunctionEvaluationHelper(template);

                foreach (var outputKey in template.Outputs.Keys.ToList())
                {
                    try
                    {
                        template.Outputs[outputKey].Value.Value = ExpressionsEngine.EvaluateLanguageExpressionsRecursive(
                            root: template.Outputs[outputKey].Value.Value,
                            evaluationContext: evaluationHelper.EvaluationContext);
                    }
                    catch (Exception)
                    {
                        template.Outputs[outputKey].Value.Value = new JValue("NOT_PARSED");
                    }
                }
            }

            return template;
        }

        /// <summary>
        /// Gets the template expression evaluation helper.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <returns>The template expression evaluation helper</returns>
        private TemplateExpressionEvaluationHelper GetTemplateFunctionEvaluationHelper(Template template)
        {
            var helper = new TemplateExpressionEvaluationHelper();

            var functionsLookup = template.GetFunctionDefinitions().ToInsensitiveDictionary(keySelector: function => function.Key, elementSelector: function => function.Function);

            var parametersLookup = template.Parameters.CoalesceEnumerable().ToInsensitiveDictionary(
                keySelector: parameter => parameter.Key,
                elementSelector: parameter => parameter.Value.Value);

            var variablesLookup = template.Variables.CoalesceEnumerable().ToInsensitiveDictionary(
                keySelector: variable => variable.Key,
                elementSelector: variable => variable.Value);

            helper.Initialize(
                metadata: template.Metadata,
                functionsLookup: functionsLookup,
                parametersLookup: parametersLookup,
                variablesLookup: variablesLookup);

            // Set reference lookup
            helper.OnGetTemplateReference = (TemplateReference templateReference) =>
            {
                foreach (var resource in template.Resources)
                {
                    if (new[] { resource.Name.Value, resource.OriginalName }.Contains(templateReference.ResourceId))
                    {
                        return resource.Properties?.Value;
                    }
                }
                return ExpressionsEngine.EvaluateLanguageExpressionsRecursive(
                    root: templateReference.ResourceId,
                    evaluationContext: helper.EvaluationContext);
            };

            return helper;
        }

        private TemplateResource[] ReorderResourceCopies(Template template, Dictionary<string, (string, int)> copyNameMap)
        {
            // Set OriginalName back on resources that were copied and reorder the resources.
            // Omit extra copies of resources if needed.
            List<TemplateResource> updatedOrder = new List<TemplateResource>();
            for (int i = 0; i < template.Resources.Length; i++)
            {
                var resource = template.Resources[i];
                if (resource.Copy == null)
                {
                    // non-copied resource.  Add to updated array
                    updatedOrder.Add(resource);
                }
                else
                {
                    // Copied resource.  Update OriginalName and:
                    // - if it's the first copy, insert back where it was supposed to be
                    // - if it's an extra copy, add to the end, or don't add at all if requested
                    if (copyNameMap.TryGetValue(resource.Copy.Name.Value, out (string, int) originalValues))
                    {
                        resource.OriginalName = originalValues.Item1;
                        if (resource.CopyContext.CopyIndex == 0)
                        {
                            updatedOrder.Insert(originalValues.Item2, resource);
                        }
                        else if (!_dropResourceCopies)
                        {
                            updatedOrder.Add(resource);
                        }
                    }
                    else
                    {
                        // Couldn't get original values.  Insert at end as a precaution,
                        // but this code should never be reached under normal circumstances.
                        updatedOrder.Add(resource);
                    }
                }
            }
            
            return updatedOrder.ToArray();
        }

        private void SetOriginalResourceNames(Template template)
        {
            foreach (var resource in template.Resources)
            {
                resource.OriginalName = resource.Name.Value;
            }
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
        /// Populates the parameters data object.
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
