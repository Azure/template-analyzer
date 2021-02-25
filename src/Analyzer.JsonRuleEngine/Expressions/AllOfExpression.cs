// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents an allOf expression in a JSON rule.
    /// </summary>
    internal class AllOfExpression : Expression
    {
        /// <summary>
        /// Gets the expressions to be evaluated.
        /// </summary>
        public Expression[] AllOf { get; private set; }

        /// <summary>
        /// Gets the type of resource to evaluate.
        /// </summary>
        public string ResourceType { get; private set; }

        /// <summary>
        /// Gets the JSON path to evaluate.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Creates an <see cref="AllOfExpression"/>.
        /// </summary>
        /// <param name="expressions">List of expressions to perform a logical AND against.</param>
        /// <param name="path">The JSON path being evaluated.</param>
        /// <param name="resourceType">The resource type this expression evaluates.</param>
        public AllOfExpression(Expression[] expressions, string path = null, string resourceType = null)
        {
            this.AllOf = expressions ?? throw new ArgumentNullException(nameof(expressions));
            this.Path = path;
            this.ResourceType = resourceType;
        }

        /// <summary>
        /// Evaluates all expressions provided and aggregates them in a final <see cref="JsonRuleEvaluation"/>.
        /// </summary>
        /// <param name="jsonScope">The json to evaluate.</param>
        /// <returns>An <see cref="JsonRuleEvaluation"/> with zero or more results of the evaluation, depending on whether there are any/multiple resources of the given type,
        /// and if the path contains any wildcards.</returns>
        public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
        {
            if (jsonScope == null)
            {
                throw new ArgumentNullException(nameof(jsonScope));
            }

            List<IJsonPathResolver> scopesToEvaluate = new List<IJsonPathResolver>();

            if (!string.IsNullOrEmpty(ResourceType))
            {
                scopesToEvaluate.AddRange(jsonScope.ResolveResourceType(ResourceType));
            }
            else
            {
                scopesToEvaluate.Add(jsonScope);
            }

            List<JsonRuleEvaluation> jsonRuleEvaluations = new List<JsonRuleEvaluation>();

            bool evaluationPassed = true;

            foreach (var scope in scopesToEvaluate)
            {
                List<IJsonPathResolver> innerScope = new List<IJsonPathResolver>();

                if (string.IsNullOrEmpty(Path))
                {
                    innerScope.Add(scope);
                }
                else
                {
                    innerScope.AddRange(scope?.Resolve(Path));
                }

                foreach (var propertyToEvaluate in innerScope)
                {
                    foreach (var expression in AllOf)
                    {
                        var evaluation = expression.Evaluate(propertyToEvaluate);
                        if (!evaluation.Passed && evaluationPassed)
                        {
                            evaluationPassed = false;
                        }

                        jsonRuleEvaluations.Add(evaluation);
                    }
                }
            }

            return new JsonRuleEvaluation(evaluationPassed, jsonRuleEvaluations);
        }
    }
}
