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
        /// <param name="jsonLineNumberResolver">An <c>IJsonLineNumberResolver</c> to
        /// map JSON paths in evaluation results to the line number in the JSON evaluated.</param>
        /// <param name="operator">The operator used to evaluate the resource type and/or path.</param>
        /// <param name="resourceType">The resource type this expression evaluates.</param>
        /// <param name="path">The JSON path being evaluated.</param>
        public LeafExpression(ILineNumberResolver jsonLineNumberResolver, LeafExpressionOperator @operator, string resourceType, string path)
            : base(resourceType, path ?? throw new ArgumentNullException(nameof(path)))
        {
            this.jsonLineNumberResolver = jsonLineNumberResolver ?? throw new ArgumentNullException(nameof(jsonLineNumberResolver));
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
        /// <returns>A <see cref="JsonRuleEvaluation"/> with the result of the evaluation.</returns>
        public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
        {
            return EvaluateInternal(
                jsonScope,
                getResult: scope =>
                {
                    var result = new JsonRuleResult()
                    {
                        Passed = Operator.EvaluateExpression(scope.JToken),
                        JsonPath = scope.Path,
                        Expression = this
                    };

                    try
                    {
                        result.LineNumber = this.jsonLineNumberResolver.ResolveLineNumber(result.JsonPath);
                    }
                    catch (Exception) { }

                    return result;
                });
        }
    }
}
