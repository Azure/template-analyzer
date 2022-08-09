// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Azure.ResourceManager.Resources.Models;
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
        /// <param name="usePowerShell">Whether or not to use PowerShell rules to analyze the template.</param>
        /// <param name="logger">A logger to report errors and debug information</param>
        /// <returns>A new <see cref="TemplateAnalyzer"/> instance.</returns>
        public static TemplateAnalyzer Create(bool usePowerShell, ILogger logger = null)
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
                usePowerShell ? new PowerShellRuleEngine(logger) : null,
                logger);
        }

        /// <summary>
        /// Runs the TemplateAnalyzer logic given the template and parameters passed to it.
        /// </summary>
        /// <param name="template">The template contents.</param>
        /// <param name="parameters">The parameters for the template.</param>
        /// <param name="templateFilePath">The template file path.</param>
        public IEnumerable<IEvaluation> AnalyzeTemplate(string template, string parameters = null, string templateFilePath = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

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
            IEnumerable<IEvaluation>  evaluations = AnalyzeAllIncludedTemplates(template, parameters, templateFilePath, template, 0, isBicep, sourceMap);

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
        /// <param name="template">The ARM Template JSON</param>
        /// <param name="parameters">The parameters for the ARM Template JSON</param>
        /// <param name="templateFilePath">The ARM Template file path</param>
        /// <param name="populatedTemplate">The ARM Template JSON with inherited parameters, variables, and functions, if applicable</param>
        /// <param name="lineNumberOffset">The offset for line numbers relative to parent templates representing where the template starts in the file. (Used for nested templates.)</param>
        /// <param name="isBicep">Whether this template was originally a Bicep file</param>
        /// <param name="sourceMap">Source map that maps ARM JSON back to source Bicep</param>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        private IEnumerable<IEvaluation> AnalyzeAllIncludedTemplates(string template, string parameters, string templateFilePath, string populatedTemplate, int lineNumberOffset, bool isBicep, object sourceMap)
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
                OriginalTemplate = JObject.Parse(template),
                ExpandedTemplate = templatejObject,
                IsMainTemplate = true,
                ResourceMappings = armTemplateProcessor.ResourceMappings,
                TemplateIdentifier = templateFilePath,
                IsBicep = isBicep,
                SourceMap = sourceMap,
                Offset = lineNumberOffset
            };

            try
            {
                IEnumerable<IEvaluation> evaluations = this.jsonRuleEngine.AnalyzeTemplate(templateContext);

                if (this.powerShellRuleEngine != null && templateContext.TemplateIdentifier != null)
                {
                    this.logger?.LogDebug("Running PowerShell rule engine");
                    evaluations = evaluations.Concat(this.powerShellRuleEngine.AnalyzeTemplate(templateContext));
                }
               
                // Recursively handle nested templates 
                dynamic jsonTemplate = JsonConvert.DeserializeObject(template);
                dynamic processedTemplateResources = templatejObject["resources"];
                dynamic processedTemplateResourcesWithLineNumbers = templateContext.OriginalTemplate["resources"];

                for (int i = 0; i < processedTemplateResources.Count; i++)
                {
                    dynamic currentProcessedResource = processedTemplateResources[i];
                    dynamic currentProcessedResourceWithLineNumbers = processedTemplateResourcesWithLineNumbers[i];

                    if (currentProcessedResource.type == "Microsoft.Resources/deployments")
                    {
                        dynamic nestedTemplateWithLineNumbers = currentProcessedResourceWithLineNumbers.properties.template;
                        dynamic populatedNestedTemplate = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(nestedTemplateWithLineNumbers));
                        // get the offset
                        int nextLineNumberOffset = (nestedTemplateWithLineNumbers as IJsonLineInfo).LineNumber + lineNumberOffset - 1; // off by one
                        // Check whether scope is set to inner or outer
                        var scope = currentProcessedResourceWithLineNumbers.properties.expressionEvaluationOptions?.scope;

                        int startOfTemplate = (nestedTemplateWithLineNumbers as IJsonLineInfo).LineNumber;
                        string jsonNestedTemplate = ExtractNestedTemplate(template, startOfTemplate);
                        IEnumerable<IEvaluation> result;

                        if (scope == null)
                        {
                            scope = "outer";
                        }
                        if (scope == "inner")
                        {
                            // Pass parameters, variables and functions to child template
                            JToken passedParameters = currentProcessedResource.properties.parameters;
                            JToken passedVariables = currentProcessedResource.properties.variables;
                            JToken passedFunctions = currentProcessedResource.properties.functions;

                            // merge 
                            populatedNestedTemplate.variables?.Merge(passedVariables);
                            populatedNestedTemplate.functions?.Merge(passedFunctions);

                            dynamic currentPassedParameter = passedParameters?.First;
                            while (currentPassedParameter != null)
                            {
                                var value = currentPassedParameter.Value.value;
                                if (value != null)
                                {
                                    currentPassedParameter.Value.defaultValue = value;
                                    currentPassedParameter.Value.Remove("value");
                                }
                                currentPassedParameter = currentPassedParameter.Next;
                            }
                            populatedNestedTemplate.parameters?.Merge(passedParameters);
                            
                            string jsonPopulatedNestedTemplate = JsonConvert.SerializeObject(populatedNestedTemplate);
                                                      
                            result = AnalyzeAllIncludedTemplates(jsonNestedTemplate, parameters, templateFilePath, jsonPopulatedNestedTemplate, nextLineNumberOffset, isBicep, sourceMap);
                        }
                        else
                        {
                            // Variables, parameters and functions inherited from parent template
                            populatedNestedTemplate.variables = jsonTemplate.variables;
                            populatedNestedTemplate.parameters = jsonTemplate.parameters;
                            populatedNestedTemplate.functions = jsonTemplate.functions;

                            string jsonPopulatedNestedTemplate = JsonConvert.SerializeObject(populatedNestedTemplate);
                            
                            result = AnalyzeAllIncludedTemplates(jsonNestedTemplate, parameters, templateFilePath, jsonPopulatedNestedTemplate, nextLineNumberOffset, isBicep, sourceMap);
                        }
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

        /// <summary>
        /// Extracts a nested template in the exact format as user input, accounting for all white space and formatting
        /// </summary>
        /// <param name="template">Parent template containing nested template</param>
        /// <param name="startOfTemplate">Line number where the nested template starts</param>
        /// <returns>A nested template string</returns>
        private static string ExtractNestedTemplate(string template, int startOfTemplate)
        {
            bool startOfNestingFound = false;
            int lineNumberCounter = 1;
            int curlyBraceCounter = 0;
            string stringNestedTemplate = "";
            foreach (var line in template.Split(Environment.NewLine))
            {
                if (lineNumberCounter < startOfTemplate)
                {
                    lineNumberCounter += 1;
                    continue;
                }
                if (!startOfNestingFound)
                {
                    if (myString.Contains('{'))
                    {
                        stringNestedTemplate += myString.Substring(myString.IndexOf('{'));
                        stringNestedTemplate += Environment.NewLine;
                        startOfNestingFound = true;
                        lineNumberCounter += 1;
                        curlyBraceCounter += 1;
                    }
                    continue;
                }
                // After finding the start of nesting, count the opening and closing braces till they match up to find the end of the nested template
                int inlineCounter = 1;
                foreach (char c in myString)
                {
                    if (c == '{') curlyBraceCounter++;
                    if (c == '}') curlyBraceCounter--;
                    if (curlyBraceCounter == 0) // done
                    {
                        stringNestedTemplate += myString[..inlineCounter];
                        break;
                    }
                    inlineCounter++;
                }

                if (curlyBraceCounter == 0)
                {
                    break;
                }

                //not done
                stringNestedTemplate += myString + Environment.NewLine;
                lineNumberCounter += 1;
            }

            return stringNestedTemplate;
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
