// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine;
using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
using Microsoft.Azure.Templates.Analyzer.BicepProcessor;
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
        private string TemplateFilePath { get; }
        private string Template { get; }
        private string Parameters { get; }
        private bool IsBicep { get; }

        /// <summary>
        /// Creates a new instance of a TemplateAnalyzer
        /// </summary>
        /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
        /// <param name="templateFilePath">The ARM Template file path. Needed to run arm-ttk checks.</param> TODO improve TemplateAnalyzer interface
        public TemplateAnalyzer(string template, string parameters = null, string templateFilePath = null)
        {
            this.Template = template ?? throw new ArgumentNullException(paramName: nameof(template));
            this.Parameters = parameters;
            this.TemplateFilePath = templateFilePath;
            this.IsBicep = templateFilePath.ToLower().EndsWith(".bicep");
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
                ArmTemplateProcessor armTemplateProcessor = IsBicep 
                        ? new BicepTemplateProcessor(Template, TemplateFilePath).ToArmTemplateProcessor()
                        : new ArmTemplateProcessor(Template);
                templatejObject = armTemplateProcessor.ProcessTemplate(Parameters);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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

                if (TemplateFilePath != null)
                {
                    evaluations = evaluations.Concat(PowerShellRuleEngine.EvaluateRules(TemplateFilePath));
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
            return System.IO.File.ReadAllText("Rules/BuiltInRules.json");
        }
    }
}
