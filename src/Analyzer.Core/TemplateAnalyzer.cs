// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine;
using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Core
{
    /// <summary>
    /// This class runs the TemplateAnalyzer logic given the template and parameters passed to it
    /// </summary>
    public class TemplateAnalyzer
    {
        private string Template { get; }
        private string Parameters { get; }

        /// <summary>
        /// Creates a new instance of a TemplateAnalyzer
        /// </summary>
        /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
        public TemplateAnalyzer(string template, string parameters = null)
        {
            this.Template = template ?? throw new ArgumentNullException(paramName: nameof(template));
            this.Parameters = parameters;
        }

        /// <summary>
        /// Runs the TemplateAnalyzer logic given the template and parameters passed to it.
        /// </summary>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        public IEnumerable<IEvaluation> EvaluateRulesAgainstTemplate()
        {
            JToken templatejObject;

            try
            {
                ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(Template);
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

                IEnumerable<IEvaluation> evaluations = jsonRuleEngine.EvaluateRules(
                    new TemplateContext {
                        OriginalTemplate = JObject.Parse(Template),
                        ExpandedTemplate = templatejObject,
                        IsMainTemplate = true },
                    rules);

                return evaluations;
            }
            catch (Exception e)
            {
                throw new TemplateAnalyzerException("Error while evaluating rules.", e);
            }
        }

        private static string LoadRules()
        {
            return System.IO.File.ReadAllText("Rules/BuiltInRules.json");
        }
    }
}
