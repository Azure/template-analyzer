// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation;
using Microsoft.Azure.Templates.Analyzer.BicepProcessor;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;
using PSRule.Definitions;
using PSRule.Pipeline;
using PSRule.Rules;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <summary>
    /// Processes the output of the PSRule analysis.
    /// </summary>
    internal sealed class PSRuleHostContext : HostContext
    {
        private readonly TemplateContext templateContext;
        private readonly ILogger logger;
        private readonly ISourceLocationResolver sourceLocationResolver;

        /// <summary>
        /// Evaluations outputted by the PSRule analysis.
        /// </summary>
        public List<PowerShellRuleEvaluation> Evaluations = new();

        /// <summary>
        /// Creates a new instance of a PSRuleHostContext.
        /// </summary>
        /// <param name="templateContext">The context of the template under analysis.</param>
        /// <param name="logger">A logger to report errors and debug information.</param>
        public PSRuleHostContext(TemplateContext templateContext, ILogger logger = null)
        {
            this.templateContext = templateContext;
            this.logger = logger;
            this.sourceLocationResolver = templateContext.IsBicep
                ? new BicepSourceLocationResolver(templateContext)
                : new JsonSourceLocationResolver(templateContext);
        }

        /// <inheritdoc/>
        public override ActionPreference GetPreferenceVariable(string variableName)
        {
            return variableName.Equals("VerbosePreference", System.StringComparison.OrdinalIgnoreCase) ? ActionPreference.Continue : base.GetPreferenceVariable(variableName);
        }

        /// <inheritdoc/>
        public override bool ShouldProcess(string target, string action)
        {
            return true;
        }

        /// <inheritdoc/>
        public override void Error(ErrorRecord errorRecord)
        {
            logger?.LogError("Error running PowerShell rules: {error}", errorRecord.Exception.Message);
        }

        /// <inheritdoc/>
        public override void Warning(string text)
        {
            logger?.LogWarning(text);
        }

        /// <inheritdoc/>
        public override void Information(InformationRecord informationRecord)
        {
            if (informationRecord?.MessageData is HostInformationMessage info)
            {
                logger?.LogDebug(info.Message);
            }
        }

        /// <inheritdoc/>
        public override void Verbose(string text)
        {
            logger?.LogDebug(text);
        }

        /// <inheritdoc/>
        public override void Record(IResultRecord record)
        {
            var ruleRecord = (RuleRecord)record;

            var ruleId = ruleRecord.Ref;
            var ruleName = ruleRecord.RuleName;
            var ruleShortDescription = ruleRecord.Info.DisplayName;
            var ruleFullDescription = ruleRecord.Info.Description.Text;
            var recommendation = ruleRecord.Recommendation;
            var helpUri = ruleRecord?.Info?.GetOnlineHelpUrl();
            var severity = ruleRecord.Level switch
            {
                PSRule.Definitions.Rules.SeverityLevel.Error => Severity.High,
                PSRule.Definitions.Rules.SeverityLevel.Warning => Severity.Medium,
                _ => Severity.Low
            };

            foreach (var reason in ruleRecord.Detail.Reason)
            {
                SourceLocation sourceLocation;

                // Temporary try/catch because not all rule evaluations return a proper path yet:
                try
                {
                    sourceLocation = this.sourceLocationResolver.ResolveSourceLocation(reason.FullPath);
                }
                catch
                {
                    sourceLocation = new SourceLocation(templateContext.TemplateIdentifier, 1);
                }
               
                this.Evaluations.Add(new PowerShellRuleEvaluation(ruleId, ruleName, helpUri, ruleShortDescription, ruleFullDescription, recommendation,
                    templateContext.TemplateIdentifier, false, severity, new Result(false, sourceLocation))); 
            }
        }
    }
}