// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// An operator that evaluates the "in" JSON expression.
    /// </summary>
    internal class InOperator : LeafExpressionOperator
    {
        /// <summary>
        /// Gets the name of this operator.
        /// </summary>
        public override string Name => "In";

        /// <summary>
        /// Creates an InOperator.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule, it has to be an array of basic JSON values.</param>
        /// <param name="isNegative">Whether the result of <see cref="EvaluateExpression(JToken)"/> should be negated or not.</param>
        public InOperator(JArray specifiedValue, bool isNegative)
        {
            this.SpecifiedValue = specifiedValue;
            this.IsNegative = isNegative;
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it is included in the specified list.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate.</param>
        /// <returns>A value indicating whether or not the evaluation passed.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            var equalsOperator = new EqualsOperator(tokenToEvaluate, isNegative: false);

            foreach (var arrayElement in this.SpecifiedValue)
            {
                if (equalsOperator.EvaluateExpression(arrayElement))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
