// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// Whether or not to run all the rules against the template.
        /// </summary>
        private readonly bool runAllRules;

        /// <summary>
        /// Logger to report errors and debug information.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Creates a new instance of a PowerShellRuleEngine.
        /// </summary>
        /// <param name="runAllRules">Whether or not to run all the rules against the template.</param>
        /// <param name="logger">A logger to report errors and debug information.</param>
        public PowerShellRuleEngine(bool runAllRules, ILogger logger = null)
        {
            this.runAllRules = runAllRules;
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

            if (templateContext?.ResourceMappings == null)
            {
                throw new ArgumentException($"{nameof(TemplateContext.ResourceMappings)} must not be null.", nameof(templateContext));
            }

            // TODO: Temporary work-around: write template to disk so PSRule will analyze it
            var tempFile = Path.GetTempFileName();
            var tempTemplateFile = Path.ChangeExtension(tempFile, ".json");
            File.WriteAllText(tempTemplateFile, templateContext.ExpandedTemplate.ToString());

            PSRuleHostContext hostContext;

            try
            {
                hostContext = new PSRuleHostContext(templateContext, logger);
                var modules = new string[] { "PSRule.Rules.Azure" };
                var optionsForFileAnalysis = new PSRuleOption
                {
                    Input = new InputOption
                    {
                        Format = InputFormat.File
                    },
                    Output = new OutputOption
                    {
                        Outcome = RuleOutcome.Fail,
                        Culture = new string[] { "en-US" } // To avoid warning messages when running tests in Linux
                    },
                    Include = new IncludeOption
                    {
                        Path = new string[]
                        {
                            ".ps-rule",
                            Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "baselines", "SecurityBaseline.Rule.json"),
                            Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "baselines", "RepeatedRulesBaseline.Rule.json")
                        }
                    },
                    Execution = new ExecutionOption
                    {
                        NotProcessedWarning = false
                    }
                };
                var resources = templateContext.ExpandedTemplate.InsensitiveToken("resources").Values<JObject>();

                var builder = CommandLineBuilder.Invoke(modules, optionsForFileAnalysis, hostContext);
                builder.InputPath(new string[] { tempTemplateFile });
                if (runAllRules)
                {
                    builder.Baseline(BaselineOption.FromString("RepeatedRulesBaseline"));
                }
                else
                {
                    builder.Baseline(BaselineOption.FromString("SecurityBaseline"));
                }

                var pipeline = builder.Build();
                pipeline.Begin();
                foreach (var resource in resources)
                {
                    pipeline.Process(resource);
                }
                pipeline.End();
            }
            finally
            {
                File.Delete(tempTemplateFile);
            }

            return hostContext.Evaluations;
        }
    }
}