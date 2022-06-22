// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

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

        private readonly Regex regex;

        /// <summary>
        /// Creates a RegexOperator.
        /// </summary>
        /// <param name="regexPattern">The regex pattern specified in the JSON rule.</param>
        /// <param name="isNegative">Whether to negate the result of the regex match.</param>
        public RegexOperator(string regexPattern, bool isNegative = false)
        {
            this.SpecifiedValue = regexPattern ?? throw new ArgumentNullException(nameof(regexPattern));
            this.IsNegative = isNegative;

            this.RegexPattern = regexPattern;

            try
            {
                this.regex = new Regex(this.RegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            catch (System.ArgumentException e)
            {
                throw new System.ArgumentException($"Regex pattern is not valid.", e);
            }
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it matches the specified regex pattern.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken to evaluate.</param>
        /// <returns>A value indicating whether or not the evaluation passed.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            if (tokenToEvaluate == null || tokenToEvaluate.Type != JTokenType.String)
            {
                return this.IsNegative;
            }

            return regex.IsMatch(tokenToEvaluate.Value<string>()) ^ this.IsNegative;
        }
    }
}