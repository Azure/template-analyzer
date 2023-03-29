// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PSRule.Configuration;
using PSRule.Pipeline;
using PSRule.Rules;
using Powershell = System.Management.Automation.PowerShell; // There's a conflict between this class name and a namespace

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <summary>
    /// Executes template analysis encoded in PowerShell.
    /// </summary>
    public class PowerShellRuleEngine : IRuleEngine
    {
        /// <summary>
        /// The name of the module containing the PowerShell rules.
        /// </summary>
        private const string PSRuleModuleName = "PSRule.Rules.Azure";

        /// <summary>
        /// Whether or not to run also non-security rules against the template.
        /// </summary>
        private readonly bool includeNonSecurityRules;

        /// <summary>
        /// Logger to report errors and debug information.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Creates a new instance of a PowerShellRuleEngine.
        /// </summary>
        /// <param name="includeNonSecurityRules">Whether or not to run also non-security rules against the template.</param>
        /// <param name="logger">A logger to report errors and debug information.</param>
        public PowerShellRuleEngine(bool includeNonSecurityRules, ILogger logger = null)
        {
            this.includeNonSecurityRules = includeNonSecurityRules;
            this.logger = logger;

            // We need to unblock the PowerShell scripts on Windows to allow them to run.
            // If a script is not unblocked, even if it's signed, PowerShell prompts for confirmation before executing.
            // This prompting would throw an exception, because there's no interaction with a user that would allow for confirmation.
            if (Platform.IsWindows)
            {
                try
                {
                    UnblockRules();
                }
                catch(Exception exception)
                {
                    this.logger?.LogError(exception, "There was an exception while unblocking the PowerShell scripts");
                }
            }
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
                var modules = new string[] { PSRuleModuleName };
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
                        NotProcessedWarning = false,

                        // PSRule internally creates a PowerShell initial state with InitialSessionState.CreateDefault().
                        // SessionState.Minimal causes PSRule to use CreateDefault2 instead of CreateDefault.
                        InitialSessionState = PSRule.Configuration.SessionState.Minimal
                    }
                };
                var resources = templateContext.ExpandedTemplate.InsensitiveToken("resources").Values<JObject>();

                var builder = CommandLineBuilder.Invoke(modules, optionsForFileAnalysis, hostContext);
                builder.InputPath(new string[] { tempTemplateFile });
                if (includeNonSecurityRules)
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

        /// <summary>
        /// Unblocks the PSRule PowerShell scripts by deleting the Zone.Identifier Alternate Data Stream.
        /// See https://learn.microsoft.com/powershell/module/microsoft.powershell.utility/unblock-file#example-3-find-and-unblock-scripts for more information.
        /// Creates a new Alternate Data Stream on the .psd1 module file to indicate that the rules have been unblocked.
        /// </summary>
        private void UnblockRules()
        {
            var rulesDirectory = Path.Combine(AppContext.BaseDirectory, "Modules", PSRuleModuleName);

            // Check if rules have already been unblocked
            var moduleFileAlternateDataStream = Path.Combine(rulesDirectory, $"{PSRuleModuleName}.psd1:Unblocked");
            if (File.Exists(moduleFileAlternateDataStream))
            {
                return;
            }

            string[] ruleFiles = Directory.GetFiles(rulesDirectory, "*.ps1", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MatchCasing = MatchCasing.CaseInsensitive
            });

            // Delete the Zone.Identifier Alternate Data Stream on each rule file
            foreach (string ruleFile in ruleFiles)
            {
                File.Delete($"{ruleFile}:Zone.Identifier");
            }

            // Create a new Alternate Data Stream on the .psd1 module file to indicate that the rules have been unblocked
            File.WriteAllBytes(moduleFileAlternateDataStream, new byte[0]);
        }
    }
}