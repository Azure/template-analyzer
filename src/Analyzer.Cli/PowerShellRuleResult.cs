// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <inheritdoc/>
    internal class PowerShellRuleResult : IResult
    {
        /// <inheritdoc/>
        public bool Passed { get; internal set; }

        /// <inheritdoc/>
        public int LineNumber { get; internal set; }

        /// <summary>
        /// Gets or sets the object related to the result. It could be a variable name, a value, etc.
        /// </summary>
        public dynamic TargetObject { get; internal set; } // FIXME set proper type if we care about this info, .Name and .Value

        /// <summary>
        /// Creates an <see cref="PowerShellRuleResult"/> that represents a result obtained from a PowerShell rule.
        /// </summary>
        /// <param name="passed">Determines whether or not the rule for this result passed.</param>
        /// <param name="lineNumber">The line number of the file where the rule was evaluated.</param>
        /// <param name="targetObject">The object related to the result.</param>
        public PowerShellRuleResult(bool passed, int lineNumber, dynamic targetObject)
        {
            Passed = passed;
            LineNumber = lineNumber;
            TargetObject = targetObject;
        }
    }
}