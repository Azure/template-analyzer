// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents a structured expression in a JSON rule.
    /// </summary>
    internal class StructuredExpression : Expression
    {
        /// <summary>
        /// Determines the boolean operation to use when evaluating the expressions.
        /// </summary>
        private readonly Func<bool, bool, bool> Operation;

        /// <summary>
        /// Gets or sets the expressions to be evaluated.
        /// </summary>
        internal Expression[] Expressions { get; set; }

        /// <summary>
        /// Creates a <see cref="StructuredExpression"/>.
        /// </summary>
        /// <param name="expressions">List of expressions to perform a logical operation against.</param>
        /// <param name="operation">The boolean operation to perform to calculate the overall expression result.</param>
        /// <param name="commonProperties">The properties common across all <see cref="Expression"/> types.</param>
        public StructuredExpression(Expression[] expressions, Func<bool,bool, bool> operation, ExpressionCommonProperties commonProperties)
            : base(commonProperties)
        {
            Expressions = expressions ?? throw new ArgumentNullException(nameof(expressions));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        /// <summary>
        /// Evaluates all expressions provided and aggregates them in a final <see cref="JsonRuleEvaluation"/>
        /// </summary>
        /// <param name="jsonScope">The json to evaluate.</param>
        /// <param name="jsonLineNumberResolver">An <see cref="ILineNumberResolver"/> to
        /// map JSON paths in the returned evaluation to the line number in the JSON evaluated.</param>
        /// <returns>An <see cref="IEnumerable{JsonRuleEvaluation}"/> with the results of the evaluation.</returns>
        public override IEnumerable<JsonRuleEvaluation> Evaluate(IJsonPathResolver jsonScope, ILineNumberResolver jsonLineNumberResolver)
        {
            return EvaluateInternal(jsonScope, scope =>
            {
                List<JsonRuleEvaluation> jsonRuleEvaluations = new List<JsonRuleEvaluation>();
                bool? evaluationPassed = null;

                foreach (var expression in Expressions)
                {
                    foreach (var evaluation in expression.Evaluate(scope, jsonLineNumberResolver))
                    {
                        // Add evaluations if scopes were found to evaluate
                        if (evaluation.HasResults)
                        {
                            evaluationPassed = !evaluationPassed.HasValue
                                // if no value, this is the first expression evaluated so set the inital value of result
                                ? evaluation.Passed
                                // otherwise use defined operation to calculate intermediate expression result
                                : Operation(evaluationPassed.Value, evaluation.Passed);

                            jsonRuleEvaluations.Add(evaluation);
                        }
                    }
                }

                evaluationPassed |= jsonRuleEvaluations.Count == 0;

                return new JsonRuleEvaluation(this, evaluationPassed.Value, jsonRuleEvaluations);
            });
        }
    }
}