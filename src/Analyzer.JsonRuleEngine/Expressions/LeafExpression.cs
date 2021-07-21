// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents a leaf expression in a JSON rule.
    /// </summary>
    internal class LeafExpression : Expression
    {
        private readonly ILineNumberResolver jsonLineNumberResolver;

        /// <summary>
        /// Creates a LeafExpression.
        /// </summary>
        /// <param name="jsonLineNumberResolver">An <see cref="ILineNumberResolver"/> to
        /// map JSON paths in evaluation results to the line number in the JSON evaluated.</param>
        /// <param name="operator">The operator used to evaluate the resource type and/or path.</param>
        /// <param name="commonProperties">The properties common across all <see cref="Expression"/> types.
        /// <see cref="ExpressionCommonProperties.Path"/> must not be null.</param>
        public LeafExpression(ILineNumberResolver jsonLineNumberResolver, LeafExpressionOperator @operator, ExpressionCommonProperties commonProperties)
            : base(commonProperties)
        {
            this.jsonLineNumberResolver = jsonLineNumberResolver ?? throw new ArgumentNullException(nameof(jsonLineNumberResolver));
            this.Operator = @operator ?? throw new ArgumentNullException(nameof(@operator));

            if (commonProperties.Path == null) throw new ArgumentException("Path property must not be null.", nameof(commonProperties));
        }

        /// <summary>
        /// Gets the leaf operator evaluating the resource type and/or path of this expression.
        /// </summary>
        public LeafExpressionOperator Operator { get; private set; }

        /// <summary>
        /// Evaluates this leaf expression's resource type and/or path, starting at the specified json scope, with the contained <see cref="LeafExpressionOperator"/>.
        /// </summary>
        /// <param name="jsonScope">The json path to evaluate.</param>
        /// <returns>A <see cref="JsonRuleEvaluation"/> with the result of the evaluation.</returns>
        public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
        {
            return EvaluateInternal(jsonScope, scope =>
            {
                var result = new JsonRuleResult()
                {    
                    Passed = Operator.EvaluateExpression(scope.JToken),
                    JsonPath = scope.Path,
                    LineNumber = this.jsonLineNumberResolver.ResolveLineNumber(scope.Path),
                    Expression = this
                };

                return result;
            });
        }
    }
}
