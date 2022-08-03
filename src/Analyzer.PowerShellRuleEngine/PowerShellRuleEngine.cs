// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PSRule.Configuration;
using PSRule.Pipeline;
using PSRule.Rules;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <summary>
    /// Executes template analysis encoded in PowerShell.
    /// </summary>
    public class PowerShellRuleEngine : IRuleEngine
    {
        /// <summary>
        /// Logger to report errors and debug information.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Creates a new instance of a PowerShellRuleEngine.
        /// </summary>
        /// <param name="logger">A logger to report errors and debug information.</param>
        public PowerShellRuleEngine(ILogger logger = null)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Analyzes a template against the rules encoded in PowerShell.
        /// </summary>
        /// <param name="templateContext">The context of the template under analysis.</param>
        /// <returns>The <see cref="IEvaluation"/>s of the PowerShell rules against the template.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(TemplateContext templateContext)
        {
            if (templateContext?.TemplateIdentifier == null)
            {
                throw new ArgumentException($"{nameof(TemplateContext.TemplateIdentifier)} must not be null.", nameof(templateContext));
            }

            if (templateContext?.ExpandedTemplate == null)
            {
                throw new ArgumentException($"{nameof(TemplateContext.ExpandedTemplate)} must not be null.", nameof(templateContext));
            }

            // TODO: Temporary work-around: write template to disk so PSRule will analyze it
            var tempTemplateFile = Path.GetTempFileName();
            File.WriteAllText(tempTemplateFile, templateContext.ExpandedTemplate.ToString());
            var templateFile = tempTemplateFile.Replace(".tmp", ".json");
            File.Move(tempTemplateFile, templateFile);

            var hostContext = new PSRuleHostContext(templateContext, logger);
            var outputOption = new OutputOption
            {
                Outcome = RuleOutcome.Fail
            };
            var modules = new string[] { "PSRule.Rules.Azure" };

            // Run PSRule on full template, for template-level rules to execute:
            var optionsForFileAnalysis = new PSRuleOption
            {
                Input = new InputOption
                {
                    Format = InputFormat.File
                },
                Output = outputOption
            };
            var resources = templateContext.ExpandedTemplate.InsensitiveToken("resources").Values<JObject>();
            var builder = CommandLineBuilder.Invoke(modules, optionsForFileAnalysis, hostContext);
            builder.InputPath(new string[] { templateFile });
            var pipeline = builder.Build();
            pipeline.Begin();
            foreach (var resource in resources)
                pipeline.Process(resource);

            pipeline.End();

            // Remove temporary file:
            File.Delete(templateFile);

            return hostContext.Evaluations;
        }
    }
}