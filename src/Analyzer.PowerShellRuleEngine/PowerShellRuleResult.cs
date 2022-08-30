// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <inheritdoc/>
    public class PowerShellRuleResult : IResult
    {
        /// <inheritdoc/>
        public bool Passed { get; internal set; }

        /// <inheritdoc/>
        public SourceLocation SourceLocation { get; internal set; }

        /// <summary>
        /// Creates a <see cref="PowerShellRuleResult"/> that represents a result obtained from a PowerShell rule.
        /// </summary>
        /// <param name="passed">Whether or not the rule for this result passed.</param>
        /// <param name="lineNumber">The line number of the file where the rule was evaluated.</param>
        public PowerShellRuleResult(bool passed, int lineNumber)
        {
            Passed = passed;
            SourceLocation = new SourceLocation(lineNumber);
        }
    }
}