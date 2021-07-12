// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents an AnyOf expression in a JSON rule.
    /// </summary>
    internal class AnyOfExpression : CompoundExpression
    {
        /// <summary>
        /// Gets the expressions to be evaluated.
        /// </summary>
        public Expression[] AnyOf
        {
            get => Expressions;
            private set => Expressions = value;
        }

        /// <summary>
        /// Creates an <see cref="AnyOfExpression"/>.
        /// </summary>
        /// <param name="expressions">List of expressions to perform a logical OR against.</param>
        /// <param name="commonProperties">The properties common across all <see cref="Expression"/> types.</param>
        public AnyOfExpression(Expression[] expressions, ExpressionCommonProperties commonProperties)
            : base(expressions, (x, y) => x || y, commonProperties)
        {
        }
    }
}
