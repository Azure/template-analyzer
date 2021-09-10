// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// The base class for all Expressions in JSON rules.
    /// </summary>
    internal abstract class Expression
    {
        /// <summary>
        /// Gets the type of resource to evaluate.
        /// </summary>
        public string ResourceType { get; private set; }

        /// <summary>
        /// Gets the JSON path to evaluate.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the Where condition of this <see cref="Expression"/>.
        /// </summary>
        public Expression Where { get; private set; }

        /// <summary>
        /// Initialization for the base Expression.
        /// </summary>
        /// <param name="commonProperties">The properties common across all <see cref="Expression"/> types.</param>
        internal Expression(ExpressionCommonProperties commonProperties)
        {
            (this.ResourceType, this.Path, this.Where) = (commonProperties.ResourceType, commonProperties.Path, commonProperties.Where);
        }

        /// <summary>
        /// Executes this <see cref="Expression"/> against a template.
        /// </summary>
        /// <param name="jsonScope">The specific scope to evaluate.</param>
        /// <returns>A <see cref="JsonRuleEvaluation"/> with the results.</returns>
        public abstract JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope);

        /// <summary>
        /// Performs tasks common across <see cref="Expression"/> implementations, such as
        /// determining all paths to run against.
        /// </summary>
        /// <param name="jsonScope">The scope being evaluated.</param>
        /// <param name="getResult">A delegate for logic specific to the child <see cref="Expression"/>, which
        /// generates a <see cref="JsonRuleResult"/> for the specified <paramref name="jsonScope"/>.</param>
        /// <returns>A <see cref="JsonRuleEvaluation"/> populated with the results generated from <paramref name="getResult"/>.</returns>
        protected JsonRuleEvaluation EvaluateInternal(IJsonPathResolver jsonScope, Func<IJsonPathResolver, JsonRuleResult> getResult) =>
            EvaluateInternal(jsonScope, getResult: getResult, getEvaluation: null);

        /// <summary>
        /// Performs tasks common across <see cref="Expression"/> implementations, such as
        /// determining all paths to run against.
        /// </summary>
        /// <param name="jsonScope">The scope being evaluated.</param>
        /// <param name="getEvaluation">A delegate for logic specific to the child <see cref="Expression"/>, which
        /// generates a <see cref="JsonRuleEvaluation"/> for the specified <paramref name="jsonScope"/>.</param>
        /// <returns>A <see cref="JsonRuleEvaluation"/>, either populated with the evaluations generated from <paramref name="getEvaluation"/>,
        /// or the single <see cref="JsonRuleEvaluation"/> if only one was generated from <paramref name="getEvaluation"/>.</returns>
        protected JsonRuleEvaluation EvaluateInternal(IJsonPathResolver jsonScope, Func<IJsonPathResolver, JsonRuleEvaluation> getEvaluation) =>
            EvaluateInternal(jsonScope, getEvaluation: getEvaluation, getResult: null);

        /// <summary>
        /// Performs tasks common across <see cref="Expression"/> implementations, such as
        /// determining all paths to run against.  Only <paramref name="getEvaluation"/>
        /// or <paramref name="getResult"/> should be populated, not both.
        /// </summary>
        /// <param name="jsonScope">The scope being evaluated.</param>
        /// <param name="getEvaluation">A delegate for logic specific to the child <see cref="Expression"/>, which
        /// generates a <see cref="JsonRuleEvaluation"/> for the specified <paramref name="jsonScope"/>.</param>
        /// <param name="getResult">A delegate for logic specific to the child <see cref="Expression"/>, which
        /// generates a <see cref="JsonRuleResult"/> for the specified <paramref name="jsonScope"/>.</param>
        /// <returns>The results of the evaluation.</returns>
        private JsonRuleEvaluation EvaluateInternal(
            IJsonPathResolver jsonScope,
            Func<IJsonPathResolver, JsonRuleEvaluation> getEvaluation,
            Func<IJsonPathResolver, JsonRuleResult> getResult)
        {
            if (jsonScope == null)
            {
                throw new ArgumentNullException(nameof(jsonScope));
            }

            // Select resources of given type, if specified
            IEnumerable<IJsonPathResolver> scopesToEvaluate;
            if (!string.IsNullOrEmpty(ResourceType))
            {
                scopesToEvaluate = jsonScope.ResolveResourceType(ResourceType);
            }
            else
            {
                scopesToEvaluate = new[] { jsonScope };
            }

            List<JsonRuleEvaluation> jsonRuleEvaluations = new List<JsonRuleEvaluation>();
            List<JsonRuleResult> jsonRuleResults = new List<JsonRuleResult>();
            bool evaluationPassed = true;

            foreach (var initialScope in scopesToEvaluate)
            {
                // Expand with path if specified
                IEnumerable<IJsonPathResolver> expandedScopes = Path == null ?
                    new[] { initialScope }.AsEnumerable() :
                    initialScope?.Resolve(Path);

                foreach (var propertyToEvaluate in expandedScopes)
                {
                    // Evaluate this path if either (a) there is no Where condition to evaluate, or (b) the Where expression passed for this path.
                    var whereEvaluation = Where?.Evaluate(propertyToEvaluate);
                    if (whereEvaluation == null || (whereEvaluation.Passed && whereEvaluation.HasResults))
                    {
                        // Expression implementation will generate Evaluation or Result.
                        // evaluationPassed is &&'d with the Passed outcome to combine
                        // the evaluations of all relevant paths (which can come from
                        // matching resource types and/or wildcards in Path).
                        if (getEvaluation != null)
                        {
                            var evaluation = getEvaluation(propertyToEvaluate);
                            evaluationPassed &= evaluation.Passed;
                            evaluation.Expression = this;
                            jsonRuleEvaluations.Add(evaluation);
                        }
                        else
                        {
                            var result = getResult(propertyToEvaluate);
                            evaluationPassed &= result.Passed;
                            result.Expression = this;
                            jsonRuleResults.Add(result);
                        } 
                    }
                }
            }

            // Return Evaluation that wraps the Expression's results.
            // If Expression resulted in a single Evaluation, return that directly.
            return getResult != null
                ? new JsonRuleEvaluation(this, evaluationPassed, jsonRuleResults)
                : jsonRuleEvaluations.Count == 1
                    ? jsonRuleEvaluations.First()
                    : new JsonRuleEvaluation(this, evaluationPassed, jsonRuleEvaluations);
        }
    }
}
