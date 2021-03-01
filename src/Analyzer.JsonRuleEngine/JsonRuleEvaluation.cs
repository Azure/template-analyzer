// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                if (Results == null)
                {
                    return null;
                }

                return resultsEvaluatedTrue ??= GetCachedResults().FindAll(r => r.Passed);
            }
            private set 
            {
                resultsEvaluatedTrue = value;
            } 
        }

        /// <summary>
        /// Gets the collections of results evaluated to false from this evaluation.
        /// </summary>
        public IEnumerable<IResult> ResultsEvaluatedFalse
        {
            get
            {
                if (Results == null)
                {
                    return null;
                }

                return resultsEvaluatedFalse ??= GetCachedResults().FindAll(r => !r.Passed);
            }
            private set
            {
                resultsEvaluatedFalse = value;
            }
        }

        /// <summary>
        /// Gets the collections of evaluations evaluated to true from this evaluation.
        /// </summary>
        public IEnumerable<IEvaluation> EvaluationsEvaluatedTrue
        {
            get
            {
                if (Evaluations == null)
                {
                    return null;
                }

                return evaluationsEvaluatedTrue ??= GetCachedEvaluationss().FindAll(r => r.Passed);
            }
            private set
            {
                evaluationsEvaluatedTrue = value;
            }
        }

        /// <summary>
        /// Gets the collections of evaluations evaluated to false from this evaluation.
        /// </summary>
        public IEnumerable<IEvaluation> EvaluationsEvaluatedFalse
        {
            get
            {
                if (Evaluations == null)
                {
                    return null;
                }

                return evaluationsEvaluatedFalse ??= GetCachedEvaluationss().FindAll(r => !r.Passed);
            }
            private set
            {
                evaluationsEvaluatedFalse = value;
            }
        }

        /// <summary>
        /// Creates an <see cref="JsonRuleEvaluation"/> that represents a structured expression.
        /// </summary>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="evaluations"><see cref="IEnumerable"/> of evaluations.</param>
        public JsonRuleEvaluation(bool passed, IEnumerable<JsonRuleEvaluation> evaluations) => (this.Passed, this.Evaluations) = (passed, evaluations);

        /// <summary>
        /// Creates an <see cref="JsonRuleEvaluation"/> that represents a leaf expression.
        /// </summary>
        /// <param name="passed">Determines whether or not the rule for this evaluation passed.</param>
        /// <param name="results"><see cref="IEnumerable"/> of results.</param>
        public JsonRuleEvaluation(bool passed, IEnumerable<JsonRuleResult> results) => (this.Passed, this.Results) = (passed, results);
    }
}