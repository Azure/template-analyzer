// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents an allOf expression in a JSON rule.
    /// </summary>
    internal class AllOfExpression : CompoundExpression
    {
        /// <summary>
        /// Gets the expressions to be evaluated.
        /// </summary>
        public Expression[] AllOf
        {
            get => Expressions;
            private set => Expressions = value;
        }

        /// <summary>
        /// Creates an <see cref="AllOfExpression"/>.
        /// </summary>
        /// <param name="expressions">List of expressions to perform a logical AND against.</param>
        /// <param name="commonProperties">The properties common across all <see cref="Expression"/> types.</param>
        public AllOfExpression(Expression[] expressions, ExpressionCommonProperties commonProperties)
            : base(expressions, (x, y) => x && y, commonProperties)
        {
        }
    }
}
