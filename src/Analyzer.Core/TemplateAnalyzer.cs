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
using Bicep.Core.SourceMapping;

namespace Microsoft.Azure.Templates.Analyzer.Core
{
    /// <summary>
    /// This class runs the TemplateAnalyzer logic given the template and parameters passed to it.
    /// </summary>
    public class TemplateAnalyzer
    {
        private string TemplateFilePath { get; }
        private string Template { get; set; }
        private SourceMap SourceMap { get; set; }
        private string Parameters { get; }
        private bool IsBicep { get; }
        private JsonRuleEngine jsonRuleEngine { get; }

        /// <summary>
        /// Private constructor to enforce use of <see cref="TemplateAnalyzer.Create"/> for creating new instances.
        /// </summary>
        /// <param name="jsonRuleEngine">The <see cref="JsonRuleEngine"/> to use in analyzing templates.</param>
        private TemplateAnalyzer(JsonRuleEngine jsonRuleEngine)
        {
            this.Template = template ?? throw new ArgumentNullException(paramName: nameof(template));
            this.Parameters = parameters;
            this.TemplateFilePath = templateFilePath;
            this.IsBicep = templateFilePath != null && templateFilePath.ToLower().EndsWith(".bicep");
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

            return new TemplateAnalyzer(
                JsonRuleEngine.Create(rules, templateContext => new JsonLineNumberResolver(templateContext)));
        }

        /// <summary>
        /// Runs the TemplateAnalyzer logic given the template and parameters passed to it.
        /// </summary>
        /// <param name="template">The ARM Template JSON</param>
        /// <param name="parameters">The parameters for the ARM Template JSON</param>
        /// <param name="templateFilePath">The ARM Template file path. (Needed to run arm-ttk checks.)</param>
        /// <param name="usePowerShell">Whether or not to use PowerShell rules to analyze the template.</param>
        /// <returns>An enumerable of TemplateAnalyzer evaluations.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(string template, string parameters = null, string templateFilePath = null, bool usePowerShell = true)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            JToken templatejObject;
            ArmTemplateProcessor armTemplateProcessor;

            try
            {
                if (IsBicep)
                {
                    (Template, SourceMap) = BicepTemplateProcessor.ConvertBicepToJson(TemplateFilePath);
                }
                armTemplateProcessor = new ArmTemplateProcessor(Template);
                templatejObject = armTemplateProcessor.ProcessTemplate(Parameters);
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
				IsBicep = IsBicep,
                SourceMap = SourceMap
            };

            try
            {

                IEnumerable<IEvaluation> evaluations = jsonRuleEngine.AnalyzeTemplate(templateContext);

                if (usePowerShell && templateContext.TemplateIdentifier != null)
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
    }
}
