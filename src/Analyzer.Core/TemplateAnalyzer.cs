// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine;
using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Core
{
    /// <summary>
    /// This class runs the TemplateAnalyzer logic given the template and parameters passed to it.
    /// </summary>
    public class TemplateAnalyzer
    {
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
                JsonRuleEngine.Create(rules, templateContext => new JsonLineNumberResolver(templateContext)),
                usePowerShell ? new PowerShellRuleEngine() : null,
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
                IsMainTemplate = true, // currently not using this
                ResourceMappings = armTemplateProcessor.ResourceMappings,
                TemplateIdentifier = templateFilePath, 
                Offset = offset
            };

            try
            {
                IEnumerable<IEvaluation> evaluations = this.jsonRuleEngine.AnalyzeTemplate(templateContext, this.logger);

                if (this.powerShellRuleEngine != null && templateContext.TemplateIdentifier != null)
                {
                    this.logger?.LogDebug("Running PowerShell rule engine");
                    evaluations = evaluations.Concat(this.powerShellRuleEngine.AnalyzeTemplate(templateContext, this.logger));
                }

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
                            int startOfTemplate = (nestedTemplateWithLineNumbers as IJsonLineInfo).LineNumber;
                            //int endOfTemplate = 0;

                            // ------------THIS IS THE END OF GETTING USER FORMATTED STRINGS
                            string stringNestedTemplate = JsonConvert.SerializeObject(nestedTemplate, Formatting.Indented);
                            string stringModifiedNestedTemplate = JsonConvert.SerializeObject(modifiedNestedTemplate, Formatting.Indented);

                            //---- MAKING REGEX WORK

                            Regex rx = new Regex(@"{[a-zA-Z_][a-zA-Z0-9_]*}\{{{?<BR>\{}|{?<-BR>\}}|[^{}]*}+\}");

                            var match = rx.Match("{}{}{}{}{}{}{}");
                            var match2 = rx.Match("{al i want is loyalty}");

                            var str = match.Value;

                            //-----MAKING REGEX WORK
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
                            string stringNestedTemplate = JsonConvert.SerializeObject(nestedTemplate, Formatting.Indented);
                            string stringModifiedNestedTemplate = JsonConvert.SerializeObject(modifiedNestedTemplate, Formatting.Indented);
                            
                            IEnumerable<IEvaluation> result = DeepAnalyzeTemplate(stringNestedTemplate, parameters, templateFilePath, stringModifiedNestedTemplate, nextOffset); // TO DO: FIND OFFSET NUMBER

                            evaluations = evaluations.Concat(result);
                        }                       
                    }
                }
                foreach (var evaluation in evaluations)
                {
                    //evaluation.Result.LineNumber += offset;  // TRY TO MAKE THIS NOT READONLY BECAUSE OTHERWISE IT HELL!!!
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
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
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
