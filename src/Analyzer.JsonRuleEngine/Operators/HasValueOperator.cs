// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// An operator that evaluates the "hasValue" JSON expression.
    /// </summary>
    internal class HasValueOperator : LeafExpressionOperator
    {
        /// <summary>
        /// Gets the name of this operator.
        /// </summary>
        public override string Name => "HasValue";

        /// <summary>
        /// Gets the effective value this operator will compare against.
        /// </summary>
        public bool EffectiveValue { get; private set; }

        /// <summary>
        /// Creates a HasValueOperator.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule.</param>
        /// <param name="isNegative">Whether the result of <see cref="EvaluateExpression(JToken)"/> should be negated or not.</param>
        public HasValueOperator(bool specifiedValue, bool isNegative)
        {
            (this.SpecifiedValue, this.IsNegative) = (specifiedValue, isNegative);

            this.EffectiveValue = specifiedValue;
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it has a value.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate.</param>
        /// <returns>A value indicating whether or not the evaluation passed.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            if (tokenToEvaluate == null || tokenToEvaluate.Type == JTokenType.Null)
            {
                return !EffectiveValue;
            }

            if (tokenToEvaluate.Type == JTokenType.String
                && tokenToEvaluate.Value<string>().Length == 0)
            {
                return !EffectiveValue;
            }

            return EffectiveValue;
        }
    }
}
