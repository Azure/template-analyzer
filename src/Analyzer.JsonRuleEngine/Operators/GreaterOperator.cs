// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// An operators that evaluates the "greater" and "less" JSON expressions.
    /// </summary>
    internal class GreaterOperator : LeafExpressionOperator
    {
        /// <inheritdoc/>
        public override string Name => this.IsNegative ? "Less" : "Greater";

        /// <summary>
        /// Creates a GreaterOperator.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule.</param>
        /// <param name="isNegative">Whether the result of <see cref="EvaluateExpression(JToken)"/> should be negated or not.</param>
        public GreaterOperator(JToken specifiedValue, bool isNegative)
        {
            this.SpecifiedValue = specifiedValue ?? throw new ArgumentNullException(nameof(specifiedValue));
            this.IsNegative = isNegative;
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it is greater or less than the specified value.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate.</param>
        /// <returns>A value indicating whether or not the evaluation passed.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            if (tokenToEvaluate == null)
            {
                // If the specified property in the JSON is not defined then we would assume it could potentially have an undesired value
                return false;
            }

            var normalizedSpecifiedValue = NormalizeValue(this.SpecifiedValue);
            var normalizedTokenToEvaluate = NormalizeValue(tokenToEvaluate);

            // TODO catch exception earlier:
            if (normalizedSpecifiedValue.Type != normalizedTokenToEvaluate.Type)
            {
                throw (new Exception(""));
            }

            // TODO improve:
            Boolean result;
            if (normalizedSpecifiedValue.Type == JTokenType.Date)
            {
                result = normalizedSpecifiedValue.Value<DateTime>() > normalizedTokenToEvaluate.Value<DateTime>();
            }
            else
            {
                result = normalizedSpecifiedValue.Value<float>() > normalizedTokenToEvaluate.Value<float>();
            }

            if (this.IsNegative)
            {
                result = !result;
            }

            return result;
        }

        private JToken NormalizeValue(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Integer => JToken.FromObject(token.Value<float>()),
                // TODO normalize the date? Add format to docs?
                _ => token,
            };
        }
    }
}