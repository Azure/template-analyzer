// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Constants;
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
        public InOperator(JArray specifiedValue)
        {
            this.SpecifiedValue = specifiedValue;
            this.IsNegative = false;
            this.FailureMessage = $"{this.Name} {JsonRuleEngineConstants.InFailureMessage}";
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it is included in the specified list.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate.</param>
        /// <returns>A value indicating whether or not the evaluation passed.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            if (tokenToEvaluate == null)
            {
                return false;
            }

            var equalsOperator = new EqualsOperator(tokenToEvaluate, isNegative: this.IsNegative);

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
