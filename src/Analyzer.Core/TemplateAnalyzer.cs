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
    /// This class runs the TemplateAnalyzer logic given the template and parameters passed to it.
    /// </summary>
    public class TemplateAnalyzer
    {
        private JsonRuleEngine jsonRuleEngine { get; }

        /// <summary>
        /// Private constructor to enforce use of <see cref="TemplateAnalyzer.Create"/> for creating new instances.
        /// </summary>
        /// <param name="jsonRuleEngine">The <see cref="JsonRuleEngine"/> to use in analyzing templates.</param>
        private TemplateAnalyzer(JsonRuleEngine jsonRuleEngine)
        {
            this.jsonRuleEngine = jsonRuleEngine;
        }

        /// <summary>
        /// Creates a new <see cref="TemplateAnalyzer"/> instance with the default built-in rules.
        /// </summary>
        public static TemplateAnalyzer Create()
        {
            string rules;
            try
            {
                rules = LoadRules();
                //var filteredRules = FilterRules(rules, jsonRuleEngine);
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException($"Failed to read rules.", e);
            }

            return new TemplateAnalyzer(
                JsonRuleEngine.Create(rules, templateContext => new JsonLineNumberResolver(templateContext)));
        }

        /// <summary>
        /// Runs the TemplateAnalyzer logic given the template and parameters passed to it.
        /// </summary>
        /// <param name="template">The ARM Template JSON</param>
        /// <param name="parameters">The parameters for the ARM Template JSON</param>
        /// <param name="configurations">The configurations for the Template Analyzer <c>JSON</c></param>
        /// <param name="templateFilePath">The ARM Template file path. (Needed to run arm-ttk checks.)</param>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(string template, string parameters = null, string configurations = null, string templateFilePath = null)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            JToken templatejObject;
            var armTemplateProcessor = new ArmTemplateProcessor(template);

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
                TemplateIdentifier = templateFilePath
            };

            try
            {
                IEnumerable<IEvaluation> evaluations = jsonRuleEngine.AnalyzeTemplate(templateContext);

                if (templateContext.TemplateIdentifier != null)
                {
                    var powerShellRuleEngine = new PowerShellRuleEngine();
                    evaluations = evaluations.Concat(powerShellRuleEngine.AnalyzeTemplate(templateContext));
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
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "Rules/BuiltInRules.json"));
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

            //// Check in default directory if no configurations parameter was passed
            //if (Configurations == null)
            //{
            //    var defaultPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
            //        "/Configurations/Configuration.json";
            //    if (Path.GetFileName(defaultPath) != null)
            //        Configurations = defaultPath;
            //    else
            //        return rules;
            //}

            //// Read file and filter rules
            //var inputs = File.ReadAllText(Configurations);

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
