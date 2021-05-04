// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// The base class for a concrete implementation of an operator in a JSON rule expression
    /// </summary>
    internal abstract class LeafExpressionOperator
    {
        /// <summary>
        /// Gets or sets the name of the operator
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets or sets whether the result of the Operator should be negated
        /// </summary>
        public bool IsNegative { get; set; }

        /// <summary>
        /// Gets or sets the expected value specified in the rule
        /// </summary>
        public JToken SpecifiedValue { get; set; }

        /// <summary>
        /// Gets the failure message when the operator does not pass
        /// </summary>
        public string FailureMessage { get; set; }

        /// <summary>
        /// Evaluates the specified JToken using the defined operation of this <c>Operator</c>.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate</param>
        /// <returns></returns>
        public abstract bool EvaluateExpression(JToken tokenToEvaluate);
    }
}
