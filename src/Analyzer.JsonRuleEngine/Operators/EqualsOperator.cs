// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// An operators that evaluates the "equals" and "notEquals" JSON expressions.
    /// </summary>
    internal class EqualsOperator : LeafExpressionOperator
    {
        /// <inheritdoc/>
        public override string Name => this.IsNegative ? "NotEquals" : "Equals";

        /// <summary>
        /// Creates an EqualsOperator.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule.</param>
        /// <param name="isNegative">Whether the result of <see cref="EvaluateExpression(JToken)"/> should be negated or not.</param>
        public EqualsOperator(JToken specifiedValue, bool isNegative)
        {
            this.SpecifiedValue = specifiedValue;
            this.IsNegative = isNegative;
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it equals (or does not equal) the specified value.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate.</param>
        /// <returns>A value indicating whether or not the evaluation passed.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            var normalizedSpecifiedValue = NormalizeValue(this.SpecifiedValue);
            var normalizedTokenToEvaluate = NormalizeValue(tokenToEvaluate);

            return this.IsNegative != JToken.DeepEquals(normalizedSpecifiedValue, normalizedTokenToEvaluate);
        }

        private JToken NormalizeValue(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Integer => JToken.FromObject(token.Value<float>()),
                JTokenType.String => JToken.FromObject(token.Value<string>().ToLower()),
                _ => token,
            };
        }
    }
}