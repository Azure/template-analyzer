// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <inheritdoc/>
    public class PowerShellRuleEvaluation : IEvaluation
    {
        private IEnumerable<IEvaluation> evaluations;
        private IResult directResult;

        /// <inheritdoc/>
        public string RuleId { get; }

        /// <inheritdoc/>
        public string RuleDescription { get; }

        /// <inheritdoc/>
        public string Recommendation { get; }

        /// <inheritdoc/>
        public string HelpUri { get; }

        /// <inheritdoc/>
        public Severity Severity { get; } = Severity.Medium;

        /// <inheritdoc/>
        public string FileIdentifier { get; }

        /// <inheritdoc/>
        public bool Passed { get; }

        /// <inheritdoc/>
        public IEnumerable<IEvaluation> Evaluations => evaluations;

        /// <inheritdoc/>
        public IResult Result => directResult;

        /// <inheritdoc/>
        public bool HasResults => Result != null || Evaluations.Any(e => e.HasResults);

        /// <summary>
        /// Creates an <see cref="PowerShellRuleEvaluation"/> that describes the evaluation of a PowerShell rule against an ARM template.
        /// </summary>
        /// <param name="ruleId">The id of the rule associated with this evaluation.</param>
        /// <param name="ruleDescription">The description of the rule associated with this evaluation.</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="result">The result of the evaluation.</param>
        public PowerShellRuleEvaluation(string ruleId, string ruleDescription, bool passed, PowerShellRuleResult result)
        {
            RuleId = ruleId;
            RuleDescription = ruleDescription;
            Recommendation = string.Empty;
            Passed = passed;
            this.directResult = result;
            this.evaluations = new List<IEvaluation>();
            HelpUri = "https://github.com/Azure/arm-ttk";
        }
    }
}