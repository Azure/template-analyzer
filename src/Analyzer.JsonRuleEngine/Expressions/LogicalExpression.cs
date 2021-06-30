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
    internal class LogicalExpression : Expression
    {
        /// <summary>
        /// Enumeration for supported logical operators.
        /// </summary>
        internal enum LogicalOperator
        {
            And,
            Or
        }

        /// <summary>
        /// Determines the logical operator to use when evaluating the expressions.
        /// </summary>
        internal LogicalOperator Operator;

        /// <summary>
        /// Gets or sets the expressions to be evaluated.
        /// </summary>
        internal Expression[] Expressions { get; set; }

        /// <summary>
        /// Creates an <see cref="LogicalExpression"/>.
        /// </summary>
        /// <param name="expressions">List of expressions to perform a logical operation against.</param>
        /// <param name="logicalOperator">The logical operation to perform on the expressions</param>
        /// <param name="commonProperties">The properties common across all <see cref="Expression"/> types.</param>
        public LogicalExpression(Expression[] expressions, LogicalOperator logicalOperator,  ExpressionCommonProperties commonProperties)
            : base(commonProperties)
        {
            Expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
            Operator = logicalOperator;
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

                foreach (var expression in Expressions)
                {
                    var evaluation = expression.Evaluate(scope);

                    // Add evaluations if scopes were found to evaluate
                    if (evaluation.HasResults)
                    {
                        switch (Operator)
                        {
                            case LogicalOperator.And:
                                evaluationPassed &= evaluation.Passed;
                                break;
                            case LogicalOperator.Or:
                                evaluationPassed |= evaluation.Passed;
                                break;
                        }
                        
                        jsonRuleEvaluations.Add(evaluation);
                    }
                }

                return new JsonRuleEvaluation(this, evaluationPassed, jsonRuleEvaluations);
            });
        }
    }
}