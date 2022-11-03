// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <inheritdoc/>
    [DebuggerDisplay("{RuleId}, {RuleName}")]
    public class PowerShellRuleEvaluation : IEvaluation
    {
        private IEnumerable<IEvaluation> evaluations;
        private Result directResult;

        /// <inheritdoc/>
        public string RuleId { get; }

        /// <inheritdoc/>
        public string RuleName { get; }

        /// <inheritdoc/>
        public string RuleDescription { get; }

        /// <inheritdoc/>
        public string Recommendation { get; }

        /// <inheritdoc/>
        public string HelpUri { get; }

        /// <inheritdoc/>
        public Severity Severity { get; }

        /// <inheritdoc/>
        public string FileIdentifier { get; }

        /// <inheritdoc/>
        public bool Passed { get; }

        /// <inheritdoc/>
        public IEnumerable<IEvaluation> Evaluations => evaluations;

        /// <inheritdoc/>
        public Result Result => directResult;

        /// <inheritdoc/>
        public bool HasResults => Result != null || Evaluations.Any(e => e.HasResults);

        /// <summary>
        /// Creates an <see cref="PowerShellRuleEvaluation"/> that describes the evaluation of a PowerShell rule against an ARM template.
        /// </summary>
        /// <param name="ruleId">The id of the rule associated with this evaluation.</param>
        /// <param name="ruleName">The name of the rule associated with this evaluation.</param>
        /// <param name="helpUri">A link to the online help and guidance for the rule.</param>
        /// <param name="ruleDescription">The description of the rule associated with this evaluation.</param>
        /// <param name="recommendation">The recommendation for addressing failures of the result.</param>
        /// <param name="file">The file this evaluation is for.</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="severity">Determines how severe the finding is.</param>
        /// <param name="result">The result of this evaluation.</param>
        public PowerShellRuleEvaluation(string ruleId, string ruleName, string helpUri, string ruleDescription, string recommendation, string file, bool passed, Severity severity, Result result)
        {
            RuleId = ruleId;
            RuleName = ruleName;
            RuleDescription = ruleDescription;
            Recommendation = recommendation;
            FileIdentifier = file;
            Passed = passed;
            Severity = severity;
            HelpUri = helpUri;
            directResult = result;
            evaluations = Enumerable.Empty<IEvaluation>();
        }
    }
}