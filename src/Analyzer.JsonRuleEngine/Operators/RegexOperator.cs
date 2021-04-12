// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    internal class RegexOperator : LeafExpressionOperator
    {
        /// <inheritdoc/>
        public override string Name => "Regex";

        /// <summary>
        /// Gets the regex pattern this operator will compare against.
        /// </summary>
        public string RegexPattern { get; private set; }

        /// <summary>
        /// Creates a RegexOperator.
        /// </summary>
        /// <param name="regexPattern">The regex pattern specified in the JSON rule.</param>
        /// <param name="isNegative">Whether the result of EvaluateExpression() should be negated or not.</param>
        public RegexOperator(string regexPattern, bool isNegative)
        {
            (this.SpecifiedValue, this.IsNegative) = (regexPattern, isNegative);

            this.RegexPattern = regexPattern;
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it matches the specified regex pattern.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate.</param>
        /// <returns>A value indicating whether or not the evaluation passed.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            if (tokenToEvaluate.Type != JTokenType.String)
            {
                return false;
            }

            Regex regex = new Regex(this.RegexPattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(tokenToEvaluate.Value<string>());
        }
    }
}