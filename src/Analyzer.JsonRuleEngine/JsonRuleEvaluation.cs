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
        private IEnumerable<IEvaluation> evaluationsEvaluatedTrue;
        private IEnumerable<IEvaluation> evaluationsEvaluatedFalse;

        private IEnumerable<IEvaluation> evaluations;
        private Result directResult;

        private List<IEvaluation> cachedEvaluations;

        /// <summary>
        /// Gets or sets the JSON rule this evaluation is for.
        /// </summary>
        internal RuleDefinition RuleDefinition { get; set; }

        /// <inheritdoc/>
        public string RuleId => RuleDefinition.Id;

        /// <inheritdoc/>
        public string RuleDescription => RuleDefinition.Description;

        /// <inheritdoc/>
        public string Recommendation => RuleDefinition.Recommendation;

        /// <inheritdoc/>
        public string HelpUri => RuleDefinition.HelpUri;

        /// <inheritdoc/>
        public Severity Severity => RuleDefinition.Severity;

        /// <inheritdoc/>
        public string FileIdentifier { get; internal set; }

        /// <inheritdoc/>
        public bool Passed { get; internal set; }

        /// <summary>
        /// Gets the expression associated with this evaluation
        /// </summary>
        internal Expression Expression { get; set; }

        public Result Result => directResult;

        public IEnumerable<IEvaluation> Evaluations => cachedEvaluations ??= evaluations.ToList();

        /// <summary>
        /// Whether or not there are any results associated with this <see cref="JsonRuleEvaluation"/>.
        /// </summary>
        /// <returns>True if there is a result in this <see cref="JsonRuleEvaluation"/> or in any sub-<see cref="JsonRuleEvaluation"/>.
        /// False otherwise.</returns>
        public bool HasResults => Result != null || Evaluations.Any(e => e.HasResults);

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
        /// Creates a <see cref="JsonRuleEvaluation"/> that represents a structured expression.
        /// </summary>
        /// <param name="expression">The expression associated with this evaluation</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="evaluations"><see cref="IEnumerable"/> of evaluations.</param>
        public JsonRuleEvaluation(Expression expression, bool passed, IEnumerable<JsonRuleEvaluation> evaluations)
        {
            this.evaluations = evaluations ?? throw new ArgumentNullException(nameof(evaluations));
            (this.Expression, this.Passed, this.directResult) = (expression, passed, null);
        }

        /// <summary>
        /// Creates a <see cref="JsonRuleEvaluation"/> that represents a leaf expression.
        /// </summary>
        /// <param name="expression">The expression associated with this evaluation</param>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="result">The result of a leaf evaluation.</param>
        public JsonRuleEvaluation(Expression expression, bool passed, JsonRuleResult result)
        {
            this.directResult = result ?? throw new ArgumentNullException(nameof(result));
            (this.Expression, this.Passed, this.evaluations) = (expression, passed, Enumerable.Empty<IEvaluation>());
        }
    }
}