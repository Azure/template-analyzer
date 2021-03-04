// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
{
    /// <inheritdoc/>
    internal class JsonRuleEvaluation : IEvaluation
    {
        private IEnumerable<IResult> resultsEvaluatedTrue;
        private IEnumerable<IResult> resultsEvaluatedFalse;

        private IEnumerable<IEvaluation> evaluationsEvaluatedTrue;
        private IEnumerable<IEvaluation> evaluationsEvaluatedFalse;

        private List<IEvaluation> cachedEvaluations;
        private List<IResult> cachedResults;

        /// <summary>
        /// Gets or sets the JSON rule this evaluation is for.
        /// </summary>
        internal RuleDefinition RuleDefinition { get; set; }

        /// <inheritdoc/>
        public string RuleName => RuleDefinition.Name;

        /// <inheritdoc/>
        public string RuleDescription => RuleDefinition.Description;

        /// <inheritdoc/>
        public string Recommendation => RuleDefinition.Recommendation;

        /// <inheritdoc/>
        public string HelpUri => RuleDefinition.HelpUri;

        /// <inheritdoc/>
        public string FileIdentifier { get; internal set; }

        /// <inheritdoc/>
        public bool Passed { get; internal set; }

        /// <inheritdoc/>
        public IEnumerable<IResult> Results { get; set; }

        /// <inheritdoc/>
        public IEnumerable<IEvaluation> Evaluations { get; set; }

        /// <summary>
        /// Gets the expression associated with this evaluation
        /// </summary>
        internal Expression Expression { get; set; }

        private List<IResult> GetCachedResults()
        {
            return cachedResults ??= Results?.ToList();
        }

        private List<IEvaluation> GetCachedEvaluationss()
        {
            return cachedEvaluations ??= Evaluations?.ToList();
        }

        /// <summary>
        /// Gets the collections of results evaluated to true from this evaluation.
        /// </summary>
        public IEnumerable<IResult> ResultsEvaluatedTrue
        { 
            get
            {
                var results = GetCachedResults();

                if (results == null)
                {
                    return null;
                }

                return resultsEvaluatedTrue ??= results.FindAll(r => r.Passed);
            }
        }

        /// <summary>
        /// Gets the collections of results evaluated to false from this evaluation.
        /// </summary>
        public IEnumerable<IResult> ResultsEvaluatedFalse
        {
            get
            {
                var results = GetCachedResults();

                if (results == null)
                {
                    return null;
                }

                return resultsEvaluatedFalse ??= results.FindAll(r => !r.Passed);
            }
        }

        /// <summary>
        /// Gets the collections of evaluations evaluated to true from this evaluation.
        /// </summary>
        public IEnumerable<IEvaluation> EvaluationsEvaluatedTrue
        {
            get
            {
                var evaluations = GetCachedEvaluationss();

                if (evaluations == null)
                {
                    return null;
                }

                return evaluationsEvaluatedTrue ??= evaluations.FindAll(r => r.Passed);
            }
        }

        /// <summary>
        /// Gets the collections of evaluations evaluated to false from this evaluation.
        /// </summary>
        public IEnumerable<IEvaluation> EvaluationsEvaluatedFalse
        {
            get
            {
                var evaluations = GetCachedEvaluationss();

                if (evaluations == null)
                {
                    return null;
                }

                return evaluationsEvaluatedFalse ??= evaluations.FindAll(r => !r.Passed);
            }
        }

        /// <summary>
        /// Creates an <see cref="JsonRuleEvaluation"/> that represents a structured expression.
        /// </summary>
        /// <param name="expression">The expression associated with this evaluation</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="evaluations"><see cref="IEnumerable"/> of evaluations.</param>
        public JsonRuleEvaluation(Expression expression, bool passed, IEnumerable<JsonRuleEvaluation> evaluations) => (this.Expression, this.Passed, this.Evaluations) = (expression, passed, evaluations);

        /// <summary>
        /// Creates an <see cref="JsonRuleEvaluation"/> that represents a leaf expression.
        /// </summary>
        /// <param name="expression">The expression associated with this evaluation</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="results"><see cref="IEnumerable"/> of results.</param>
        public JsonRuleEvaluation(Expression expression, bool passed, IEnumerable<JsonRuleResult> results) => (this.Expression, this.Passed, this.Results) = (expression, passed, results);
    }
}