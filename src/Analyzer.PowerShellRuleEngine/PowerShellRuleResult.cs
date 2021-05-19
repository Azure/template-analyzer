// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <inheritdoc/>
    internal class PowerShellRuleResult : IResult
    {
        /// <inheritdoc/>
        public bool Passed { get; internal set; }

        /// <inheritdoc/>
        public int LineNumber { get; internal set; }

        /// <summary>
        /// Creates an <see cref="PowerShellRuleResult"/> that represents a result obtained from a PowerShell rule.
        /// </summary>
        /// <param name="passed">Determines whether or not the rule for this result passed.</param>
        /// <param name="lineNumber">The line number of the file where the rule was evaluated.</param>
        public PowerShellRuleResult(bool passed, int lineNumber)
        {
            Passed = passed;
            LineNumber = lineNumber;
        }
    }
}