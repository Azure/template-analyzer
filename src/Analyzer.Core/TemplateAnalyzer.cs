// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.BicepProcessor;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine;
using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Core
{
    /// <summary>
    /// This class runs the TemplateAnalyzer logic given the template and parameters passed to it.
    /// </summary>
    public class TemplateAnalyzer
    {
        /// <summary>
        /// Exception message when error during Bicep template compilation.
        /// </summary>
        public static readonly string BicepCompileErrorMessage = "Error compiling Bicep template";

        private JsonRuleEngine jsonRuleEngine;
        private PowerShellRuleEngine powerShellRuleEngine;

        private ILogger logger;

        /// <summary>
        /// Private constructor to enforce use of <see cref="TemplateAnalyzer.Create"/> for creating new instances.
        /// </summary>
        /// <param name="jsonRuleEngine">The <see cref="JsonRuleEngine"/> to use in analyzing templates.</param>
        /// <param name="powerShellRuleEngine">The <see cref="PowerShellRuleEngine"/> to use in analyzing templates.</param>
        /// <param name="logger">A logger to report errors and debug information</param>
        private TemplateAnalyzer(JsonRuleEngine jsonRuleEngine, PowerShellRuleEngine powerShellRuleEngine, ILogger logger)
        {
            this.jsonRuleEngine = jsonRuleEngine;
            this.powerShellRuleEngine = powerShellRuleEngine;
            this.logger = logger;
        }

        /// <summary>
        /// Creates a new <see cref="TemplateAnalyzer"/> instance with the default built-in rules.
        /// </summary>
        /// <param name="includeNonSecurityRules">Whether or not to run also non-security rules against the template.</param>
        /// <param name="logger">A logger to report errors and debug information</param>
        /// <returns>A new <see cref="TemplateAnalyzer"/> instance.</returns>
        public static TemplateAnalyzer Create(bool includeNonSecurityRules, ILogger logger = null)
        {
            string rules;
            try
            {
                rules = LoadRules();
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException("Failed to read rules.", e);
            }

            return new TemplateAnalyzer(
                JsonRuleEngine.Create(
                    rules,
                    templateContext => templateContext.IsBicep
                        ? new BicepLocationResolver(templateContext)
                        : new JsonLineNumberResolver(templateContext),
                    logger),
                new PowerShellRuleEngine(includeNonSecurityRules, logger),
                logger);
        }

        /// <summary>
        /// Runs the TemplateAnalyzer logic given the template and parameters passed to it.
        /// </summary>
        /// <param name="template">The template contents.</param>
        /// <param name="templateFilePath">The template file path. It's needed to analyze Bicep files and to run the PowerShell based rules.</param>
        /// <param name="parameters">The parameters for the template.</param>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(string template, string templateFilePath, string parameters = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));
            if (templateFilePath == null) throw new ArgumentNullException(nameof(templateFilePath));

            // If the template is Bicep, convert to JSON and get source map:
            var isBicep = templateFilePath != null && templateFilePath.ToLower().EndsWith(".bicep", StringComparison.OrdinalIgnoreCase);
            object sourceMap = null;
            if (isBicep)
            {
                try
                {
                    (template, sourceMap) = BicepTemplateProcessor.ConvertBicepToJson(templateFilePath);
                }
                catch (Exception e)
                {
                    throw new TemplateAnalyzerException(BicepCompileErrorMessage, e);
                }
            }

            var templateContext = new TemplateContext
            {
                OriginalTemplate = null,
                ExpandedTemplate = null,
                IsMainTemplate = true,
                ResourceMappings = null,
                TemplateIdentifier = templateFilePath,
                IsBicep = isBicep,
                SourceMap = sourceMap,
                PathPrefix = "",
                ParentContext = null
            };
            var  evaluations = AnalyzeAllIncludedTemplates(template, parameters, templateFilePath, templateContext, "");

            // For each rule we don't want to report the same line more than once
            // This is a temporal fix
            var evalsToValidate = new List<IEvaluation>();
            var evalsToNotValidate = new List<IEvaluation>();
            foreach (var eval in evaluations)
            {
                if (!eval.Passed && eval.Result != null)
                {
                    evalsToValidate.Add(eval);
                }
                else
                {
                    evalsToNotValidate.Add(eval);
                }
            }
            var uniqueResults = new Dictionary<(string, int), IEvaluation>();
            foreach (var eval in evalsToValidate)
            {
                uniqueResults.TryAdd((eval.RuleId, eval.Result.LineNumber), eval);
            }
            evaluations = uniqueResults.Values.Concat(evalsToNotValidate);

            return evaluations;
        }

        /// <summary>
        /// Analyzes ARM templates, recursively going through the nested templates
        /// </summary>
        /// <param name="populatedTemplate">The ARM Template JSON with inherited parameters, variables, and functions, if applicable</param>
        /// <param name="parameters">The parameters for the ARM Template JSON</param>
        /// <param name="templateFilePath">The ARM Template file path</param>
        /// <param name="parentContext">Template context for the immediate parent template</param>
        /// <param name="pathPrefix"> Prefix for resources' path used for line number mapping in nested templates</param>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        private IEnumerable<IEvaluation> AnalyzeAllIncludedTemplates(string populatedTemplate, string parameters, string templateFilePath, TemplateContext parentContext, string pathPrefix)
        {
            JToken templatejObject;
            var armTemplateProcessor = new ArmTemplateProcessor(populatedTemplate, logger: this.logger);

            try
            {
                templatejObject = armTemplateProcessor.ProcessTemplate(parameters);
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException("Error while processing template.", e);
            }

            var templateContext = new TemplateContext
            {
                OriginalTemplate = JObject.Parse(populatedTemplate),
                ExpandedTemplate = templatejObject,
                IsMainTemplate = parentContext.ParentContext == null,
                ResourceMappings = armTemplateProcessor.ResourceMappings,
                TemplateIdentifier = templateFilePath,
                IsBicep = parentContext.IsBicep,
                SourceMap = parentContext.SourceMap,
                PathPrefix = pathPrefix,
                ParentContext = parentContext
            };

            try
            {
                IEnumerable<IEvaluation> evaluations = this.jsonRuleEngine.AnalyzeTemplate(templateContext);
                evaluations = evaluations.Concat(this.powerShellRuleEngine.AnalyzeTemplate(templateContext));

                // Recursively handle nested templates 
                var jsonTemplate = JObject.Parse(populatedTemplate);
                var processedTemplateResources = templatejObject.InsensitiveToken("resources");


                for (int i = 0; i < processedTemplateResources.Count(); i++)
                {
                    var currentProcessedResource = processedTemplateResources[i];

                    if (currentProcessedResource.InsensitiveToken("type")?.ToString().Equals("Microsoft.Resources/deployments", StringComparison.OrdinalIgnoreCase) ?? false)
                    {
                        var nestedTemplate = currentProcessedResource.InsensitiveToken("properties.template");
                        if (nestedTemplate == null)
                        {
                            continue; // This is a Linked template 
                        }
                        var populatedNestedTemplate = nestedTemplate.DeepClone();

                        // Check whether scope is set to inner or outer
                        var scope = currentProcessedResource.InsensitiveToken("properties.expressionEvaluationOptions")?.InsensitiveToken("scope")?.ToString();
                        string nextPathPrefix = nestedTemplate.Path;
                                               
                        if (scope == null || scope.Equals("outer", StringComparison.OrdinalIgnoreCase))
                        {
                            // Variables, parameters and functions inherited from parent template
                            string functionsKey = populatedNestedTemplate.InsensitiveToken("functions")?.Parent.Path ?? "functions";
                            string variablesKey = populatedNestedTemplate.InsensitiveToken("variables")?.Parent.Path ?? "variables";
                            string parametersKey = populatedNestedTemplate.InsensitiveToken("parameters")?.Parent.Path ?? "parameters" ;

                            populatedNestedTemplate[functionsKey] = jsonTemplate.InsensitiveToken("functions");
                            populatedNestedTemplate[variablesKey] = jsonTemplate.InsensitiveToken("variables");
                            populatedNestedTemplate[parametersKey] = jsonTemplate.InsensitiveToken("parameters");
                        }
                        else // scope is inner
                        {
                            // Pass variables and functions to child template
                            (populatedNestedTemplate.InsensitiveToken("variables") as JObject)?.Merge(currentProcessedResource.InsensitiveToken("properties.variables"));
                            (populatedNestedTemplate.InsensitiveToken("functions") as JObject)?.Merge(currentProcessedResource.InsensitiveToken("properties.functions)"));

                            // Pass parameters to child template as the 'parameters' argument
                            var parametersToPass = currentProcessedResource.InsensitiveToken("properties.parameters");

                            if (parametersToPass != null)
                            {
                                parametersToPass["parameters"] = parametersToPass;
                                parametersToPass["$schema"] = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#";
                                parametersToPass["contentVersion"] = "1.0.0.0";
                                parameters = JsonConvert.SerializeObject(parametersToPass);
                            }
                        }

                        string jsonPopulatedNestedTemplate = JsonConvert.SerializeObject(populatedNestedTemplate);

                        IEnumerable<IEvaluation> result = AnalyzeAllIncludedTemplates(jsonPopulatedNestedTemplate, parameters, templateFilePath, templateContext, nextPathPrefix);
                        evaluations = evaluations.Concat(result);
                    }
                }
                return evaluations;
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException("Error while evaluating rules.", e);
            }
        }

        private static string LoadRules()
        {
            return File.ReadAllText(
                Path.Combine(
                    Path.GetDirectoryName(AppContext.BaseDirectory),
                    "Rules/BuiltInRules.json"));
        }

        /// <summary>
        /// Modifies the rules to run based on values defined in the configuration file.
        /// </summary>
        /// <param name="configuration">The configuration specifying rule modifications.</param>
        public void FilterRules(ConfigurationDefinition configuration)
        {
            jsonRuleEngine.FilterRules(configuration);
        }
    }
}
