// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents a all of expression in a JSON rule.
    /// </summary>
    internal class AllOfExpression : Expression
    {
        /// <summary>
        /// Gets or sets the expressions to be evaluated.
        /// </summary>
        public Expression[] AllOf { get; private set; }

        /// <summary>
        /// Gets the type of resource to evaluate.
        /// </summary>
        public string ResourceType { get; private set; }

        /// <summary>
        /// Creates an <see cref="AllOfExpression"/>.
        /// </summary>
        /// <param name="expressions">List of expressions to perform a logical AND against.</param>
        /// <param name="resoureType">The resource type this expression evaluates.</param>
        public AllOfExpression(Expression[] expressions, string resoureType = null)
        {
            this.AllOf = expressions ?? throw new ArgumentNullException(nameof(expressions));
            this.ResourceType = resoureType;
        }

        /// <summary>
        /// Evaluates all expressions provided and aggregates them in a final <see cref="Evaluation"/>.
        /// </summary>
        /// <param name="jsonScope">The json to evaluate.</param>
        /// <returns>An <see cref="Evaluation"/> with zero or more results of the evaluation, depending on whether there are any/multiple resources of the given type,
        /// and if the path contains any wildcards.</returns>
        public override Evaluation Evaluate(IJsonPathResolver jsonScope)
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

            List<JsonRuleResult> jsonRuleResults = new List<JsonRuleResult>();
            bool evaluationPassed = true;

            foreach (var scope in scopesToEvaluate)
            {
                foreach (var expression in AllOf)
                {
                    var evaluation = expression.Evaluate(scope);
                    if (!evaluation.Passed && evaluationPassed)
                    {
                        evaluationPassed = false;
                    }

                    jsonRuleResults.AddRange(evaluation.Results);
                }
            }

            return new Evaluation(evaluationPassed, jsonRuleResults);
        }
    }
}