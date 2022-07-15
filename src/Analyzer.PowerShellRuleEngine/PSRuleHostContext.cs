// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Management.Automation;
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
        private readonly JsonLineNumberResolver jsonLineNumberResolver;

        /// <summary>
        /// Evaluations outputted by the PSRule analysis.
        /// </summary>
        public List<PowerShellRuleEvaluation> Evaluations = new();

        /// <summary>
        /// Creates a new instance of a PSRuleHostContext
        /// </summary>
        /// <param name="templateContext">The context of the template under analysis.</param>
        /// <param name="logger">A logger to report errors and debug information.</param>
        public PSRuleHostContext(TemplateContext templateContext, ILogger logger = null)
        {
            this.templateContext = templateContext;
            this.logger = logger;
            this.jsonLineNumberResolver = new JsonLineNumberResolver(templateContext);
        }

        /// <inheritdoc/>
        public override bool ShouldProcess(string target, string action)
        {
            return true;
        }

        /// <inheritdoc/>
        public override void Error(ErrorRecord errorRecord)
        {
            logger?.LogError("Error running rule: {error}", errorRecord.Exception.Message);
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
        public override void Record(IResultRecord record)
        {
            // base.Record(record); TODO double check

            var ruleRecord = (RuleRecord)record; // TODO doublecheck

            var ruleId = ruleRecord.Ref;
            var ruleName = ruleRecord.RuleName;
            var ruleDescription = ruleRecord.Info.DisplayName;
            var recommendation = ruleRecord.Recommendation;
            var severity = ruleRecord.Level switch
            {
                PSRule.Definitions.Rules.SeverityLevel.Error => Severity.High,
                PSRule.Definitions.Rules.SeverityLevel.Warning => Severity.Medium,
                _ => Severity.Low
            };

            foreach (var reason in ruleRecord.Detail.Reason)
            {
                var lineNumber = 1;
                try // Temporal, TODO improve anyways?
                {
                    lineNumber = this.jsonLineNumberResolver.ResolveLineNumber(reason.Path);
                }
                catch
                {
                }
               
                // TODO: add reason as a message into result
                this.Evaluations.Add(new PowerShellRuleEvaluation(ruleId, ruleName, ruleDescription, recommendation,
                        templateContext.TemplateIdentifier, false, severity, new PowerShellRuleResult(false, lineNumber))); 
            }
        }
    }
}