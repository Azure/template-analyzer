// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine;
using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Core
{
    /// <summary>
    /// This class runs the TemplateAnalyzer logic given the template and parameters passed to it
    /// </summary>
    public class TemplateAnalyzer
    {
        private string TemplateFilePath { get; }
        private string Configurations { get; set; }
        private string Template { get; }
        private string Parameters { get; }

        /// <summary>
        /// Creates a new instance of a TemplateAnalyzer
        /// </summary>
        /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
        /// <param name="configurations">The configurations for the Template Analyzer <c>JSON</c></param>
        /// <param name="templateFilePath">The ARM Template file path. Needed to run arm-ttk checks.</param>
        public TemplateAnalyzer(string template, string parameters = null, string configurations = null, string templateFilePath = null)
        {
            this.Template = template ?? throw new ArgumentNullException(paramName: nameof(template));
            this.Parameters = parameters;
            this.Configurations = configurations;
            this.TemplateFilePath = templateFilePath;
        }

        /// <summary>
        /// Runs the TemplateAnalyzer logic given the template and parameters passed to it.
        /// </summary>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        public IEnumerable<IEvaluation> EvaluateRulesAgainstTemplate()
        {
            JToken templatejObject;
            var armTemplateProcessor = new ArmTemplateProcessor(Template);

            try
            {
                templatejObject = armTemplateProcessor.ProcessTemplate(Parameters);
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException("Error while processing template.", e);
            }

            if (templatejObject == null)
            {
                throw new TemplateAnalyzerException("Processed Template cannot be null.");
            }

            try
            {
                var rules = LoadRules();
                
                var jsonRuleEngine = new JsonRuleEngine(context => new JsonLineNumberResolver(context));
                var filteredRules = FilterRules(rules, jsonRuleEngine);

                IEnumerable<IEvaluation> evaluations = jsonRuleEngine.EvaluateRules(
                    new TemplateContext {
                        OriginalTemplate = JObject.Parse(Template),
                        ExpandedTemplate = templatejObject,
                        IsMainTemplate = true,
                        ResourceMappings = armTemplateProcessor.ResourceMappings },
                    filteredRules);

                if (TemplateFilePath != null)
                {
                    var powerShellRuleEngine = new PowerShellRuleEngine(); //ttk
                    evaluations = evaluations.Concat(powerShellRuleEngine.EvaluateRules(TemplateFilePath));
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
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                "/Rules/BuiltInRules.json");
        }

        /// <summary>
        /// Filters the rules based on the configurations file.
        /// </summary>
        /// <param name="rules">The ARM Template unfiltered rules.</param>
        /// <param name="jsonRuleEngine">The jsonRuleEngine</param>
        /// <returns>The ARM Template filtered rules based on the configurations file.</returns>
        private string FilterRules(string rules, JsonRuleEngine jsonRuleEngine)
        {
            //this.Template =template ?? throw new ArgumentNullException(paramName: nameof(template));
            //this.Configurations = configurations;

            // Check in default directory if no configurations parameter was passed
            if (Configurations == null)
            {
                var defaultPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                    "/Configurations/Configuration.json";
                if (Path.GetFileName(defaultPath) != null)
                    Configurations = defaultPath;
                else
                    return rules;
            }

            // Read file and filter rules
            var inputs = File.ReadAllText(Configurations);

            ////if there is a filter or a change value
           // jsonRuleEngine.FilterRules();// create
            //List<RuleDefinition> rules2;
            //try
            //{
            //    rules2 = JsonConvert.DeserializeObject<List<RuleDefinition>>(rules);
            //}
            //catch (Exception e)
            //{
            //    throw new JsonRuleEngineException($"Failed to parse rules.", e);
            //}

            //foreach (RuleDefinition rule in rules)
            //{
            //    Expression ruleExpression;

            //    try
            //    {
            //        ruleExpression = rule.ExpressionDefinition.ToExpression(BuildLineNumberResolver(templateContext));
            //    }
            //    catch (Exception e)
            //    {
            //        throw new JsonRuleEngineException($"Failed to parse rule {rule.Name}.", e);
            //    }

            //    JsonRuleEvaluation evaluation = ruleExpression.Evaluate(
            //        new JsonPathResolver(
            //            templateContext.ExpandedTemplate,
            //            templateContext.ExpandedTemplate.Path));

            //    evaluation.RuleDefinition = rule;
            //    evaluation.FileIdentifier = templateContext.TemplateIdentifier;

            //    yield return evaluation;
            //}

            // Return eliminated rules
            return rules;
        }
    }
}
