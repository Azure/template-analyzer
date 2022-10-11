// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <inheritdoc/>
    public class PowerShellRuleResult : Result
    {
        /// <summary>
        /// Creates a <see cref="PowerShellRuleResult"/> that represents a result obtained from a PowerShell rule.
        /// </summary>
        /// <param name="passed">Whether or not the rule for this result passed.</param>
        /// <param name="sourceLocation">The source location where the rule was evaluated.</param>
        public PowerShellRuleResult(bool passed, SourceLocation sourceLocation) : base(passed, sourceLocation)
        {
        }
    }
}