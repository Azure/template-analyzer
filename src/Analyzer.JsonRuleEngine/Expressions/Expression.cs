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
        /// Initialization for the base Expression.
        /// </summary>
        /// <param name="resourceType">The resource type this expression evaluates.</param>
        /// <param name="path">The JSON path being evaluated.</param>
        internal Expression(string resourceType, string path)
        {
            this.ResourceType = resourceType;
            this.Path = path;
        }

        /// <summary>
        /// Performs implementation-specific evaluation against the JSON scope.
        /// </summary>
        /// <param name="jsonScope">The specific scope to evaluate.</param>
        /// <returns>The results of the evaluation, which can be either a <c>JsonRuleEvaluation</c> or a <c>JsonRuleResult</c>,
        /// but must not be both. Child classes implementing this method must always return the same type
        /// (<c>JsonRuleEvaluation</c> or a <c>JsonRuleResult</c>) for every invocation.</returns>
        protected abstract (JsonRuleEvaluation evaluation, JsonRuleResult result) EvaluateInternal(IJsonPathResolver jsonScope);

        /// <summary>
        /// Executes this <c>Expression</c> against a template.
        /// </summary>
        /// <param name="jsonScope">The specific scope to evaluate.</param>
        /// <returns>An <c>Evaluation</c> with the results.</returns>
        public JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
        {
            if (jsonScope == null)
            {
                throw new ArgumentNullException(nameof(jsonScope));
            }

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
                IEnumerable<IJsonPathResolver> expandedScopes = Path == null ?
                    new[] { initialScope }.AsEnumerable() :
                    initialScope?.Resolve(Path);

                foreach (var propertyToEvaluate in expandedScopes)
                {
                    (var evaluation, var result) = EvaluateInternal(propertyToEvaluate);
                    if (evaluation != null)
                    {
                        evaluationPassed &= evaluation.Passed;
                        evaluation.Expression = this;
                        jsonRuleEvaluations.Add(evaluation);
                    }
                    else
                    {
                        evaluationPassed &= result.Passed;
                        result.Expression = this;
                        jsonRuleResults.Add(result);
                    }
                }
            }

            return jsonRuleEvaluations.Count > 0
                ? new JsonRuleEvaluation(this, evaluationPassed, jsonRuleEvaluations)
                : new JsonRuleEvaluation(this, evaluationPassed, jsonRuleResults);
        }
    }
}
