// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// An operator that evaluates the "exists" JSON expression.
    /// </summary>
    internal class ExistsOperator : LeafExpressionOperator
    {
        /// <summary>
        /// Gets the name of this operator.
        /// </summary>
        public override string Name => "Exists";

        /// <summary>
        /// Gets the effective value this operator will compare against.
        /// </summary>
        public bool EffectiveValue { get; private set; }

        /// <summary>
        /// Creates an ExistsOperator.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule.</param>
        /// <param name="isNegative">Whether the result of <see cref="EvaluateExpression(JToken)"/> should be negated or not.</param>
        public ExistsOperator(bool specifiedValue, bool isNegative)
        {
            (this.SpecifiedValue, this.IsNegative) = (specifiedValue, isNegative);

            this.EffectiveValue = specifiedValue;
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it exists.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate.</param>
        /// <returns>A value indicating whether or not the evaluation passed.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
            => (tokenToEvaluate != null) == EffectiveValue;
    }
}
