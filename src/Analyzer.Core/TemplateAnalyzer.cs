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
        /// Exception message when error during bicep template compilation
        /// </summary>
        public static readonly string BicepCompileErrorMessage = "Error compiling bicep template";

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
        /// <param name="template">The ARM Template JSON</param>
        /// <param name="parameters">The parameters for the ARM Template JSON</param>
        /// <param name="templateFilePath">The ARM Template file path. (Needed to run arm-ttk checks.)</param>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(string template, string parameters = null, string templateFilePath = null)
        {

            return DeepAnalyzeTemplate(template, parameters, templateFilePath, template, 0);
        }

        /// <summary>
        /// Runs TemplateAnalyzer logic accounting for nested templates
        /// </summary>
        /// <param name="initialTemplate">The ARM Template JSON</param>
        /// <param name="parameters">The parameters for the ARM Template JSON</param>
        /// <param name="templateFilePath">The ARM Template file path. (Needed to run arm-ttk checks.)</param>
        /// <param name="modifiedTemplate">The ARM Template JSON with inherited parameters, variables, and functions if applicable</param>
        /// <param name="offset">The offset number for line numbers</param>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        private IEnumerable<IEvaluation> DeepAnalyzeTemplate(string initialTemplate, string parameters, string templateFilePath, string modifiedTemplate, int offset)
        {
            if (initialTemplate == null) throw new ArgumentNullException(nameof(initialTemplate));

            // if the template is bicep, convert to JSON and get source map
            var isBicep = templateFilePath != null && templateFilePath.ToLower().EndsWith(".bicep", StringComparison.OrdinalIgnoreCase);
            object sourceMap = null;
            if (isBicep)
            {
                try
                {
                    (initialTemplate, sourceMap) = BicepTemplateProcessor.ConvertBicepToJson(templateFilePath);
                }
                catch (Exception e)
                {
                    throw new TemplateAnalyzerException(BicepCompileErrorMessage, e);
                }
            }

            JToken templatejObject;
            var armTemplateProcessor = new ArmTemplateProcessor(modifiedTemplate, logger: this.logger);

            try
            {
                templatejObject = armTemplateProcessor.ProcessTemplate(parameters);
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException("Error while processing template.", e);
            }
            //IDEA
            // MAYBE PASS OFFSET NUMBER TOO SO THE NESTED TEMPLATES KNOW BY HOW MUCH TO BE OFFSET ON TOP OF LINE NUMBERS GOTTEN FROM PARSING
            // MAYBE FILE PATH TOO, MIGHT NEED TO BE LOOKED INTO 
            // ************
            // lower priority
            // TRY EXTRACTING TEMPLATE USING LINE NUMBERS AS PER VERA'S SUGGESTION TO SEE HOW THAT GOES, MIGHT MAINTAIN THE ORIGINAL STATE OF THE DOCUMENT
            // IF I EXTRACT NESTED PART FROM THE TEMPLATE STRING WITHOUT DESERIALIZING AND SERIALIZING BACK IT MIGHT PRESERVE USER STUFF
            var templateContext = new TemplateContext
            {
                OriginalTemplate = JObject.Parse(initialTemplate),
                ExpandedTemplate = templatejObject,
                IsMainTemplate = true,
                ResourceMappings = armTemplateProcessor.ResourceMappings,
                TemplateIdentifier = templateFilePath,
                IsBicep = isBicep,
                SourceMap = sourceMap,
                Offset = offset
            };

            try
            {
                IEnumerable<IEvaluation> evaluations = this.jsonRuleEngine.AnalyzeTemplate(templateContext);

                if (this.powerShellRuleEngine != null && templateContext.TemplateIdentifier != null)
                {
                    this.logger?.LogDebug("Running PowerShell rule engine");
                    evaluations = evaluations.Concat(this.powerShellRuleEngine.AnalyzeTemplate(templateContext));
                }

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


                // START OF MY EXPERIMENTAL CODE


                dynamic jsonTemplate = JsonConvert.DeserializeObject(initialTemplate);

                dynamic jsonResources = jsonTemplate.resources;
                // It seems to me like JObject.Parse(initialTemplate) is the same as templateObject but with line numbers
                dynamic processedTemplateResources = templatejObject["resources"];
                dynamic processedTemplateResourcesWithLineNumbers = templateContext.OriginalTemplate["resources"]; //.

                for (int i = 0; i < jsonResources.Count; i++)
                {
                    dynamic currentResource = jsonResources[i];
                    dynamic currentProcessedResource = processedTemplateResources[i];
                    dynamic currentProcessedResourceWithLineNumbers = processedTemplateResourcesWithLineNumbers[i]; //.

                    if (currentResource.type == "Microsoft.Resources/deployments")
                    {
                        dynamic nestedTemplate = currentResource.properties.template;
                        dynamic nestedTemplateWithLineNumbers = currentProcessedResourceWithLineNumbers.properties.template; //.
                        dynamic modifiedNestedTemplate = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(nestedTemplate));
                        // get the offset, which is the end of current template and beginning of the nested
                        int nextOffset = (nestedTemplateWithLineNumbers as IJsonLineInfo).LineNumber + offset - 1; // for off by one error
                        // check whether scope is set to inner or outer
                        var scope = currentResource.properties.expressionEvaluationOptions?.scope;
                        if (scope == null)
                        {
                            scope = "outer";
                        }
                        if (scope == "inner")
                        {
                            // allow for passing of params, variables and functions but evaluate everything else in the inner context
                            // check for params, variables and functions in parent, extract them and append them to the params, variables and functions
                            // of the child, and overwrite those of the child if needed. 
                            JToken passedParameters = currentProcessedResource.properties.parameters;
                            JToken passedVariables = currentProcessedResource.properties.variables;
                            JToken passedFunctions = currentProcessedResource.properties.functions;

                            // merge 
                            modifiedNestedTemplate.variables?.Merge(passedVariables);
                            modifiedNestedTemplate.functions?.Merge(passedFunctions);

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

                            modifiedNestedTemplate.parameters?.Merge(passedParameters);
                            //  -----------THIS IS WHERE I TRY TO GET USER-FORMATTED STRINGS
                            bool startOfNestingFound = false;
                            int lineNumberCounter = 1;
                            int curlyBraceCounter = 0;
                            int startOfTemplate = (nestedTemplateWithLineNumbers as IJsonLineInfo).LineNumber;
                            string testing = "";
                            foreach (var myString in initialTemplate.Split(Environment.NewLine))
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
                                        testing += myString.Substring(myString.IndexOf('{'));
                                        testing += Environment.NewLine;
                                        startOfNestingFound = false;
                                        lineNumberCounter += 1;
                                        curlyBraceCounter += 1;
                                    }
                                    continue;
                                }
                                // after finding the start of nesting, count the opening and closing braces till they match up
                                int inlineCounter = 1;
                                foreach (char c in myString)
                                {
                                    if (c == '{') curlyBraceCounter++;
                                    if (c == '}') curlyBraceCounter--;
                                    if (curlyBraceCounter == 0) // done
                                    {
                                        testing += myString.Substring(0, inlineCounter);
                                        break;
                                    }
                                    inlineCounter++;
                                }

                                if (curlyBraceCounter == 0)
                                {
                                    break;
                                }

                                //not done
                                testing += myString + Environment.NewLine;
                                lineNumberCounter += 1;
                            }
                            //string stringNestedTemplate = JsonConvert.SerializeObject(nestedTemplate, Formatting.Indented);
                            string stringNestedTemplate = testing;
                            string stringModifiedNestedTemplate = JsonConvert.SerializeObject(modifiedNestedTemplate, Formatting.Indented);

                            
                            IEnumerable<IEvaluation> result = DeepAnalyzeTemplate(stringNestedTemplate, parameters, templateFilePath, stringModifiedNestedTemplate, nextOffset); // TO DO: FIND OFFSET NUMBER

                            evaluations = evaluations.Concat(result);
                        }
                        else
                        {
                            // inner nested variables and params do not matter and just use whatever the parent passes down
                            // one option is to modify the structure of the nested template's varaibles and parameters to use that of the parent but this inteferes
                            // with the structure of the template itself and I am not sure that is a good thing. 
                            modifiedNestedTemplate.variables = jsonTemplate.variables;
                            modifiedNestedTemplate.parameters = jsonTemplate.parameters;
                            modifiedNestedTemplate.functions = jsonTemplate.functions;

                            //  -----------THIS IS WHERE I TRY TO GET USER-FORMATTED STRINGS
                            bool startOfNestingFound = false;
                            int lineNumberCounter = 1;
                            int curlyBraceCounter = 0;
                            int startOfTemplate = (nestedTemplateWithLineNumbers as IJsonLineInfo).LineNumber;
                            string testing = "";
                            foreach (var myString in initialTemplate.Split(Environment.NewLine))
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
                                        testing += myString.Substring(myString.IndexOf('{'));
                                        testing += Environment.NewLine;
                                        startOfNestingFound = true;
                                        lineNumberCounter += 1;
                                        curlyBraceCounter += 1;
                                    }
                                    continue;
                                }
                                // after finding the start of nesting, count the opening and closing braces till they match up
                                int inlineCounter = 1;
                                foreach (char c in myString)
                                {
                                    if (c == '{') curlyBraceCounter++;
                                    if (c == '}') curlyBraceCounter--;
                                    if (curlyBraceCounter == 0) // done
                                    {
                                        testing += myString.Substring(0, inlineCounter);
                                        break;
                                    }
                                    inlineCounter++;
                                }

                                if (curlyBraceCounter == 0)
                                {
                                    break;
                                }

                                //not done
                                testing += myString + Environment.NewLine;
                                lineNumberCounter += 1;
                            }

                            //--------------------------------------------------------
                            //string stringNestedTemplate = JsonConvert.SerializeObject(nestedTemplate, Formatting.Indented);
                            string stringNestedTemplate = testing;
                            string stringModifiedNestedTemplate = JsonConvert.SerializeObject(modifiedNestedTemplate, Formatting.Indented);
                            
                            IEnumerable<IEvaluation> result = DeepAnalyzeTemplate(stringNestedTemplate, parameters, templateFilePath, stringModifiedNestedTemplate, nextOffset); // TO DO: FIND OFFSET NUMBER

                            evaluations = evaluations.Concat(result);
                        }                     
                    }
                }

                // END OF MY EXPERIMENTAL CODE
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
