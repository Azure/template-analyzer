// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
{
    /// <summary>
    /// Describes the result of an evaluation of an expression.
    /// </summary>
    internal class Evaluation
    {
        /// <summary>
        /// Gets a value indicating whether or not the rule for this evaluation passed.
        /// </summary>
        public bool Passed { get; internal set; }

        /// <summary>
        /// A collections of results from this evaluation
        /// </summary>
        public IEnumerable<JsonRuleResult> Results { get; set; }

        private IEnumerable<JsonRuleResult> resultsEvaluatedTrue;
        private IEnumerable<JsonRuleResult> resultsEvaluatedFalse;

        /// <summary>
        /// Creates an <see cref="Evaluation"/>.
        /// </summary>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="results"><see cref="IEnumerable"/> of results.</param>
        public Evaluation(bool passed, IEnumerable<JsonRuleResult> results) => (this.Passed, this.Results) = (passed, results);

        /// <summary>
        /// Gets all the results that evaluated to true.
        /// </summary>
        /// <returns>The results that evaluated to true.</returns>
        public IEnumerable<JsonRuleResult> GetResultsEvaluatedTrue()
        {
            if (resultsEvaluatedTrue == null)
            {
                resultsEvaluatedTrue = Results.ToList().FindAll(r => r.Passed);
            }

            return resultsEvaluatedTrue;
        }

        /// <summary>
        /// Gets all the results that evaluated to false.
        /// </summary>
        /// <returns>The results that evaluated to false.</returns>
        public IEnumerable<JsonRuleResult> GetResultsEvaluatedFalse()
        {
            if (resultsEvaluatedFalse == null)
            {
                resultsEvaluatedFalse = Results.ToList().FindAll(r => !r.Passed);
            }

            return resultsEvaluatedFalse;
        }
    }
}