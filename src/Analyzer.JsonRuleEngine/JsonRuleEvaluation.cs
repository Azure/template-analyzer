// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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

        private IEnumerable<IEvaluation> evaluations;
        private IEnumerable<IResult> results;

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

        /// <summary>
        /// Gets the expression associated with this evaluation
        /// </summary>
        internal Expression Expression { get; set; }

        public IEnumerable<IResult> Results
        {
            get => cachedResults ??= results.ToList();
        }

        public IEnumerable<IEvaluation> Evaluations
        {
            get => cachedEvaluations ??= evaluations.ToList();
        }

        /// <summary>
        /// Whether or not there are any results associated with this <see cref="JsonRuleEvaluation"/>.
        /// </summary>
        /// <returns>True if there are any results in this <see cref="JsonRuleEvaluation"/> or a sub-<see cref="JsonRuleEvaluation"/>.
        /// False otherwise.</returns>
        public bool HasResults
        {
            get => Results.Any() || Evaluations.Any(e => e.HasResults);
        }

        /// <summary>
        /// Whether or not there are any evaluations associated with this <see cref="JsonRuleEvaluation"/>.
        /// </summary>
        /// <returns>True if there are any evaluations in this <see cref="JsonRuleEvaluation"/>.
        /// False otherwise.</returns>
        public bool HasEvaluations
        {
            get => Evaluations.Any();
        }

        /// <summary>
        /// Whether or not there are any results or evaluations associated with this <see cref="JsonRuleEvaluation"/>.
        /// </summary>
        /// <returns>True if there are results or evaluations in this <see cref="JsonRuleEvaluation"/> or a sub-<see cref="JsonRuleEvaluation"/>.
        /// False otherwise.</returns>
        public bool ScopesFound
        {
            get => HasResults || HasEvaluations;
        }

        /// <summary>
        /// Gets the collections of results evaluated to true from this evaluation.
        /// </summary>
        public IEnumerable<IResult> ResultsEvaluatedTrue
        { 
            get => resultsEvaluatedTrue ??= Results.ToList().FindAll(r => r.Passed);
        }

        /// <summary>
        /// Gets the collections of results evaluated to false from this evaluation.
        /// </summary>
        public IEnumerable<IResult> ResultsEvaluatedFalse
        {
            get => resultsEvaluatedFalse ??= Results.ToList().FindAll(r => !r.Passed);
        }

        /// <summary>
        /// Gets the collections of evaluations evaluated to true from this evaluation.
        /// </summary>
        public IEnumerable<IEvaluation> EvaluationsEvaluatedTrue
        {
            get => evaluationsEvaluatedTrue ??= Evaluations.ToList().FindAll(r => r.Passed);
        }

        /// <summary>
        /// Gets the collections of evaluations evaluated to false from this evaluation.
        /// </summary>
        public IEnumerable<IEvaluation> EvaluationsEvaluatedFalse
        {
            get => evaluationsEvaluatedFalse ??= Evaluations.ToList().FindAll(r => !r.Passed);
        }

        /// <summary>
        /// Creates an <see cref="JsonRuleEvaluation"/> that represents a structured expression.
        /// </summary>
        /// <param name="expression">The expression associated with this evaluation</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="evaluations"><see cref="IEnumerable"/> of evaluations.</param>
        public JsonRuleEvaluation(Expression expression, bool passed, IEnumerable<JsonRuleEvaluation> evaluations)
        {
            this.evaluations = evaluations ?? throw new ArgumentNullException(nameof(evaluations));
            (this.Expression, this.Passed, this.results) = (expression, passed, Enumerable.Empty<IResult>());
        }

        /// <summary>
        /// Creates an <see cref="JsonRuleEvaluation"/> that represents a leaf expression.
        /// </summary>
        /// <param name="expression">The expression associated with this evaluation</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="results"><see cref="IEnumerable"/> of results.</param>
        public JsonRuleEvaluation(Expression expression, bool passed, IEnumerable<JsonRuleResult> results)
        {
            this.results = results ?? throw new ArgumentNullException(nameof(results));
            (this.Expression, this.Passed, this.evaluations) = (expression, passed, Enumerable.Empty<IEvaluation>());
        }
    }
}