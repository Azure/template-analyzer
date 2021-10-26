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
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException($"Failed to read rules.", e);
            }

            JsonRuleEngine ruleEngine;
            try
            {
                ruleEngine = JsonRuleEngine.Create(rules, templateContext => new JsonLineNumberResolver(templateContext));
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException($"Failed to create a JsonRuleEngine instance.", e);
            }

            return new TemplateAnalyzer(ruleEngine);
        }

        /// <summary>
        /// Runs the TemplateAnalyzer logic given the template and parameters passed to it.
        /// </summary>
        /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
        /// <param name="templateFilePath">The ARM Template file path. Needed to run arm-ttk checks.</param>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(string template, string parameters = null, string templateFilePath = null)
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
                IEnumerable<IEvaluation> evaluations = jsonRuleEngine.EvaluateTemplate(templateContext);

                if (templateContext.TemplateIdentifier != null)
                {
                    var powerShellRuleEngine = new PowerShellRuleEngine();
                    evaluations = evaluations.Concat(powerShellRuleEngine.EvaluateTemplate(templateContext));
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
    }
}
