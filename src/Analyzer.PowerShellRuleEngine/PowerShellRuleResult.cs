// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <inheritdoc/>
    public class PowerShellRuleResult : IResult, IEquatable<PowerShellRuleResult>
    {
        /// <inheritdoc/>
        public bool Passed { get; internal set; }

        /// <inheritdoc/>
        public SourceLocation SourceLocation { get; internal set; }

        /// <summary>
        /// Creates a <see cref="PowerShellRuleResult"/> that represents a result obtained from a PowerShell rule.
        /// </summary>
        /// <param name="passed">Whether or not the rule for this result passed.</param>
        /// <param name="sourceLocation">The source location where the rule was evaluated.</param>
        public PowerShellRuleResult(bool passed, SourceLocation sourceLocation)
        {
            Passed = passed;
            SourceLocation = sourceLocation;
        }

        /// <inheritdoc/>
        public bool Equals(PowerShellRuleResult other)
        {
            return Passed.Equals(other.Passed) &&
                SourceLocation.GetActualLocation().Equals(other.SourceLocation.GetActualLocation());
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            var result = other as PowerShellRuleResult;
            return (other != null) && Equals(result);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}