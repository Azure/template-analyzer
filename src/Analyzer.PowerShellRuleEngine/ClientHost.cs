// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    // TODO rename? and add summaries

    internal sealed class ClientHost : HostContext
    {
        public List<PowerShellRuleEvaluation> Evaluations = new();

        private readonly TemplateContext templateContext;
        private readonly ILogger logger;
        private readonly JsonLineNumberResolver jsonLineNumberResolver;

        public ClientHost(TemplateContext templateContext, ILogger logger = null)
        {
            this.templateContext = templateContext;
            this.logger = logger;
            this.jsonLineNumberResolver = new JsonLineNumberResolver(templateContext);
        }

        public override bool ShouldProcess(string target, string action)
        {
            return true;
        }

        public override void Error(ErrorRecord errorRecord)
        {
            logger?.LogError("Error running rule: {error}", errorRecord.Exception.Message);
        }

        public override void Warning(string text)
        {
            logger?.LogWarning(text);
        }

        public override void Information(InformationRecord informationRecord)
        {
            if (informationRecord?.MessageData is HostInformationMessage info)
            {
                logger?.LogDebug(info.Message);
            }
        }

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