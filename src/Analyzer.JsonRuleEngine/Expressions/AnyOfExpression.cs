// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents an anyOf expression in a JSON rule.
    /// </summary>
    internal class AnyOfExpression : Expression
    {
        /// <summary>
        /// Gets or sets the expressions to be evaluated.
        /// </summary>
        public Expression[] AnyOf { get; private set; }

        /// <summary>
        /// Creates an <see cref="AnyOfExpression"/>.
        /// </summary>
        /// <param name="expressions">List of expressions to perform a logical OR against.</param>
        /// <param name="commonProperties">The properties common across all <see cref="Expression"/> types.</param>
        public AnyOfExpression(Expression[] expressions, ExpressionCommonProperties commonProperties)
            : base(commonProperties)
        {
            this.AnyOf = expressions ?? throw new ArgumentNullException(nameof(expressions));
        }

        /// <summary>
        /// Evaluates all expressions provided and aggregates them in a final <see cref="JsonRuleEvaluation"/>
        /// </summary>
        /// <param name="jsonScope">The json to evaluate.</param>
        /// <returns>A <see cref="JsonRuleEvaluation"/> with zero or more results of the evaluation, depending on whether there are any/multiple resources of the given type,
        /// and if the path contains any wildcards.</returns>
        public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
        {
            return EvaluateInternal(jsonScope, scope =>
            {
                List<JsonRuleEvaluation> jsonRuleEvaluations = new List<JsonRuleEvaluation>();
                bool evaluationPassed = false;

                foreach (var expression in AnyOf)
                {
                    var evaluation = expression.Evaluate(scope);

                    // Filter out evaluation if it didn't find the scope to evaluate
                    if (evaluation.NoScopesFound)
                    {
                        continue;
                    }

                    evaluationPassed |= evaluation.Passed;
                    jsonRuleEvaluations.Add(evaluation);
                }

                return new JsonRuleEvaluation(this, evaluationPassed, jsonRuleEvaluations);
            });
        }
    }
}