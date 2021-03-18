// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;

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
        /// Initialization for the base Expression.
        /// </summary>
        /// <param name="resourceType">The resource type this expression evaluates.</param>
        /// <param name="path">The JSON path being evaluated.</param>
        internal Expression(string resourceType, string path)
        {
            (this.ResourceType, this.Path) = (resourceType, path);
        }


        /// <summary>
        /// Executes this <c>Expression</c> against a template.
        /// </summary>
        /// <param name="jsonScope">The specific scope to evaluate.</param>
        /// <returns>An <c>Evaluation</c> with the results.</returns>
        public abstract JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope);

        /// <summary>
        /// Performs tasks common across <see cref="Expression"/> implementations, such as
        /// determining all paths to run against.  Either <paramref name="getEvaluation"/>
        /// or <paramref name="getResult"/> must be non-null, but not both.
        /// </summary>
        /// <param name="jsonScope">The scope being evaluated.</param>
        /// <param name="getEvaluation">A delegate for logic specific to the child <see cref="Expression"/>, which
        /// generates a <see cref="JsonRuleEvaluation"/> for the specified <paramref name="jsonScope"/>.  If this is
        /// specified, <paramref name="getResult"/> must be null.</param>
        /// <param name="getResult">A delegate for logic specific to the child <see cref="Expression"/>, which
        /// generates a <see cref="JsonRuleResult"/> for the specified <paramref name="jsonScope"/>.  If this is
        /// specified, <paramref name="getEvaluation"/> must be null.</param>
        /// <returns>The results of the evaluation.</returns>
        protected JsonRuleEvaluation EvaluateInternal(
            IJsonPathResolver jsonScope,
            Func<IJsonPathResolver, JsonRuleEvaluation> getEvaluation = null,
            Func<IJsonPathResolver, JsonRuleResult> getResult = null)
        {
            if (jsonScope == null)
            {
                throw new ArgumentNullException(nameof(jsonScope));
            }
            if (getEvaluation == null && getResult == null)
            {
                throw new ArgumentNullException($"{nameof(getEvaluation)} and {nameof(getResult)}");
            }
            if (getEvaluation != null && getResult != null)
            {
                throw new ArgumentException($"Only one of {nameof(getEvaluation)} and {nameof(getResult)} can be specified.");
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
                    // Expression implementation will generate Evaluation or Result
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
