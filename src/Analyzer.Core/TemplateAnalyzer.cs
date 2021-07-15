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
        private string Template { get; set; }
        private string Parameters { get; }
        private bool IsBicep { get; }

        /// <summary>
        /// Creates a new instance of a TemplateAnalyzer
        /// </summary>
        /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
        /// <param name="templateFilePath">The ARM Template file path. Needed to run arm-ttk checks.</param>
        public TemplateAnalyzer(string template, string parameters = null, string templateFilePath = null)
        {
            this.Template = template ?? throw new ArgumentNullException(paramName: nameof(template));
            this.Parameters = parameters;
            this.TemplateFilePath = templateFilePath;
            this.IsBicep = templateFilePath != null && templateFilePath.ToLower().EndsWith(".bicep");
        }

        /// <summary>
        /// Runs the TemplateAnalyzer logic given the template and parameters passed to it.
        /// </summary>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        public IEnumerable<IEvaluation> EvaluateRulesAgainstTemplate()
        {
            JToken templatejObject;
            ArmTemplateProcessor armTemplateProcessor;

            try
            {
                if (IsBicep)
                {
                    Template = BicepTemplateProcessor.ConvertBicepToJson(TemplateFilePath);
                }
                armTemplateProcessor = new ArmTemplateProcessor(Template);
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
                        IsMainTemplate = true,
                        ResourceMappings = armTemplateProcessor.ResourceMappings },
                    rules);

                if (TemplateFilePath != null)
                {
                    var powerShellRuleEngine = new PowerShellRuleEngine();
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
    }
}
