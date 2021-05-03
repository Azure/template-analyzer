// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Constants;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
{
    /// <summary>
    /// Describes the result of a TemplateAnalyzer JSON rule against an ARM template.
    /// </summary>
    internal class JsonRuleResult : IResult
    {
        /// <summary>
        /// Gets a value indicating whether or not the rule for this result passed.
        /// </summary>
        public bool Passed { get; internal set; }

        /// <summary>
        /// Gets the line number of the file where the rule was evaluated.
        /// </summary>
        public int LineNumber { get; internal set; }

        /// <summary>
        /// Gets or sets the JSON path to the location in the JSON where the rule was evaluated.
        /// </summary>
        internal string JsonPath { get; set; }

        /// <summary>
        /// Gets the expression associated with this result.
        /// </summary>
        internal Expression Expression { get; set; }

        /// <summary>
        /// Gets the actual value present at the specified path.
        /// </summary>
        internal JToken ActualValue { get; set; }

        /// <summary>
        /// Gets the messsage which explains why the evaluation failed.
        /// </summary>
        public string FailureMessage()
        {
            string failureMessage = Expression.FailureMessage;

            if (Expression is LeafExpression)
            {
                string expectedValue = ((Expression as LeafExpression).Operator.SpecifiedValue == null || (Expression as LeafExpression).Operator.SpecifiedValue.Value<string>() == null) ? "null" : (Expression as LeafExpression).Operator.SpecifiedValue.Value<string>();
                failureMessage = failureMessage.Replace(JsonRuleEngineConstants.ExpectedValuePlaceholder, expectedValue);
                failureMessage = failureMessage.Replace(JsonRuleEngineConstants.NegationPlaceholder, (Expression as LeafExpression).Operator.IsNegative ? "" : "not");
            }

            string actualValue = (ActualValue == null || ActualValue.Value<string>() == null) ? "null" : ActualValue.Value<string>();
            return failureMessage.Replace(JsonRuleEngineConstants.ActualValuePlaceholder, actualValue).Replace(JsonRuleEngineConstants.PathPlaceholder, JsonPath);
        }
    }
}
