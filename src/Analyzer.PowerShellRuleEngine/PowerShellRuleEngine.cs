// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;
using PSRule.Configuration;
using PSRule.Pipeline;
using PSRule.Rules;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <summary>
    /// Executes template analysis encoded in PowerShell.
    /// </summary>
    public class PowerShellRuleEngine : IRuleEngine
    {
        /// <summary>
        /// Logger for logging notable events.
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
        /// <param name="templateContext">The context of the template under analysis.
        /// <see cref="TemplateContext.TemplateIdentifier"/> must be the file path of the template to evaluate.</param>
        /// <returns>The <see cref="IEvaluation"/>s of the PowerShell rules against the template.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(TemplateContext templateContext)
        {
            if (templateContext?.TemplateIdentifier == null)
            {
                throw new ArgumentException($"{nameof(TemplateContext.TemplateIdentifier)} must not be null.", nameof(templateContext));
            }

            // TODO: Temporary work-around: write template to disk so PSRule will analyze it
            var tempTemplateFile = Path.GetTempFileName();
            File.WriteAllText(tempTemplateFile, templateContext.ExpandedTemplate.ToString());
            var templateFile = tempTemplateFile.Replace(".tmp", ".json");
            File.Move(tempTemplateFile, templateFile);

            var host = new ClientHost(templateContext.TemplateIdentifier, logger);
            var outputOption = new OutputOption
            {
                Outcome = RuleOutcome.Fail // TODO check if should add .Error here too or that handled by overwriting Error()?
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
            var builder = CommandLineBuilder.Invoke(modules, optionsForFileAnalysis, host);
            builder.InputPath(new string[] { PSRuleOption.GetWorkingPath() }); // TODO should be templateFile
            var pipeline = builder.Build();
            pipeline.Begin();
            pipeline.Process(null);
            pipeline.End();

            // Remove temporary file:
            File.Delete(templateFile);

            // Run PSRule on resources array, for typed-rules:
            var optionsForResourceAnalysis = new PSRuleOption
            {
                Input = new InputOption
                {
                    Format = InputFormat.Json
                },
                Output = outputOption
            };
            var resources = templateContext.ExpandedTemplate.InsensitiveToken("resources");
            builder = CommandLineBuilder.Invoke(modules, optionsForResourceAnalysis, host);
            pipeline = builder.Build();
            pipeline.Begin();
            pipeline.Process(resources.ToString());
            pipeline.End();

            return host.Evaluations;
        }
    }
}