// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <inheritdoc/>
    public class PowerShellRuleEvaluation : IEvaluation
    {
        /// <inheritdoc/>
        public string RuleName { get; }

        /// <inheritdoc/>
        public string RuleDescription { get; }

        /// <inheritdoc/>
        public string Recommendation { get; }

        /// <inheritdoc/>
        public string HelpUri { get; }

        /// <inheritdoc/>
        public string FileIdentifier { get; }

        /// <inheritdoc/>
        public bool Passed { get; }

        /// <inheritdoc/>
        public IEnumerable<IEvaluation> Evaluations { get; }

        /// <inheritdoc/>
        public IEnumerable<IResult> Results { get; }

        /// <inheritdoc/>
        public bool HasResults
        {
            get => Results.Any();
        }

        /// <summary>
        /// Creates an <see cref="PowerShellRuleEvaluation"/> that describes the evaluation of a PowerShell rule against an ARM template.
        /// </summary>
        /// <param name="ruleName">The name of the rule associated with this evaluation.</param>
        /// <param name="ruleDescription">The description of the rule associated with this evaluation.</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="results"><see cref="IEnumerable"/> of the results.</param>
        public PowerShellRuleEvaluation(string ruleName, string ruleDescription, bool passed, IEnumerable<PowerShellRuleResult> results)
        {
            RuleName = ruleName;
            RuleDescription = ruleDescription;
            Passed = passed;
            Results = results;
            Evaluations = new List<IEvaluation>();
            HelpUri = "https://github.com/Azure/arm-ttk";
        }
    }
}