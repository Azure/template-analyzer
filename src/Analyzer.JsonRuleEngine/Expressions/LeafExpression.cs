// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine
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
        /// <param name="operator">The operator used to evaluate the resource type and/or path.</param>
        public LeafExpression(string resourceType, string path, LeafExpressionOperator @operator)
            : base(resourceType, path ?? throw new ArgumentNullException(nameof(path)))
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
        /// <param name="jsonScope">The json to evaluate.</param>
        /// <returns>Zero or more results of the evaluation, depending on whether there are any/multiple resources of the given type,
        /// and if the path contains any wildcards.</returns>
        protected override JsonRuleResult EvaluateInternal(IJsonPathResolver jsonScope)
        {
            return new JsonRuleResult
            {
                Passed = Operator.EvaluateExpression(jsonScope.JToken),
                JsonPath = jsonScope.Path
            };
        }
    }
}
