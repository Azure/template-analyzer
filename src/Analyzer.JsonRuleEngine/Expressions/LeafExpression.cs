// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents a leaf expression in a JSON rule.
    /// </summary>
    internal class LeafExpression : Expression
    {
        /// <summary>
        /// Creates a LeafExpression.
        /// </summary>
        /// <param name="resourceType">The resource type this expression evaluates.</param>
        /// <param name="path">The JSON path being evaluated.</param>
        /// <param name="where">The Where condition of this expression.</param>
        /// <param name="operator">The operator used to evaluate the resource type and/or path.</param>
        public LeafExpression(string resourceType, string path, Expression where, LeafExpressionOperator @operator)
            : base(resourceType, path ?? throw new ArgumentNullException(nameof(path)), where)
        {
            this.Operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
        }

        /// <summary>
        /// Gets the leaf operator evaluating the resource type and/or path of this expression.
        /// </summary>
        public LeafExpressionOperator Operator { get; private set; }

        /// <summary>
        /// Evaluates this leaf expression's resource type and/or path, starting at the specified json scope, with the contained <c>LeafExpressionOperator</c>.
        /// </summary>
        /// <param name="jsonScope">The json path to evaluate.</param>
        /// <returns>A <see cref="JsonRuleResult"/> with the result of the evaluation.</returns>
        protected override (JsonRuleEvaluation evaluation, JsonRuleResult result) EvaluateInternal(IJsonPathResolver jsonScope)
        {
            return (
                null,
                new JsonRuleResult
                {
                    Passed = Operator.EvaluateExpression(jsonScope.JToken),
                    JsonPath = jsonScope.Path
                }
            );
        }
    }
}
