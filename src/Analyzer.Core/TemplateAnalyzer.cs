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
        /// <param name="customJsonRulesPath">An optional custom rules json file path.</param>
        /// <param name="includePowerShellRules">Whether or not to run also powershell rules against the template.</param>
        /// <returns>A new <see cref="TemplateAnalyzer"/> instance.</returns>
        public static TemplateAnalyzer Create(bool includeNonSecurityRules, ILogger logger = null, FileInfo customJsonRulesPath = null, bool includePowerShellRules = true)
        {
            string rules;
            try
            {
                rules = LoadRules(customJsonRulesPath);
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException("Failed to read rules.", e);
            }

            return new TemplateAnalyzer(
                JsonRuleEngine.Create(
                    rules,
                    templateContext => templateContext.IsBicep
                        ? new BicepSourceLocationResolver(templateContext)
                        : new JsonSourceLocationResolver(templateContext),
                    logger),
                    includePowerShellRules ? new PowerShellRuleEngine(includeNonSecurityRules, logger) : null,
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
            object bicepMetadata = null;
            if (isBicep)
            {
                try
                {
                    (template, bicepMetadata) = BicepTemplateProcessor.ConvertBicepToJson(templateFilePath);
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
                BicepMetadata = bicepMetadata,
                PathPrefix = "",
                ParentContext = null
            };

            return AnalyzeAllIncludedTemplates(template, parameters, templateFilePath, templateContext, string.Empty);
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
                IsMainTemplate = parentContext.OriginalTemplate == null, // Even the top level context will have a parent defined, but it won't represent a processed template
                ResourceMappings = armTemplateProcessor.ResourceMappings,
                TemplateIdentifier = templateFilePath,
                IsBicep = parentContext.IsBicep,
                BicepMetadata = parentContext.BicepMetadata,
                PathPrefix = pathPrefix,
                ParentContext = parentContext
            };

            try
            {
                IEnumerable<IEvaluation> evaluations = this.jsonRuleEngine.AnalyzeTemplate(templateContext);

                if (this.powerShellRuleEngine is not null)
                {
                    evaluations = evaluations.Concat(this.powerShellRuleEngine.AnalyzeTemplate(templateContext));
                }

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
                            this.logger?.LogWarning($"A linked template was found on: {templateFilePath}, linked templates are currently not supported");

                            continue;
                        }
                        var populatedNestedTemplate = nestedTemplate.DeepClone();

                        // Check whether scope is set to inner or outer
                        var scope = currentProcessedResource.InsensitiveToken("properties.expressionEvaluationOptions.scope")?.ToString();

                        if (scope == null || scope.Equals("outer", StringComparison.OrdinalIgnoreCase))
                        {
                            // Variables, parameters and functions inherited from parent template
                            string functionsKey = populatedNestedTemplate.InsensitiveToken("functions")?.Parent.Path ?? "functions";
                            string variablesKey = populatedNestedTemplate.InsensitiveToken("variables")?.Parent.Path ?? "variables";
                            string parametersKey = populatedNestedTemplate.InsensitiveToken("parameters")?.Parent.Path ?? "parameters";

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

                        IEnumerable<IEvaluation> result = AnalyzeAllIncludedTemplates(jsonPopulatedNestedTemplate, parameters, templateFilePath, templateContext, nestedTemplate.Path);
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

        private static string LoadRules(FileInfo rulesFile)
        {
            rulesFile ??= new FileInfo(Path.Combine(
                    Path.GetDirectoryName(AppContext.BaseDirectory),
                    "Rules/BuiltInRules.json"));

            using var fileStream = rulesFile.OpenRead();
            using var streamReader = new StreamReader(fileStream);

            return streamReader.ReadToEnd();
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
