// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// An operator that evaluates the "greater", "greaterOrEquals", "less", and "lessOrEquals" JSON expressions.
    /// </summary>
    internal class InequalityOperator : LeafExpressionOperator
    {
        /// <inheritdoc/>
        public override string Name => GetName();

        /// <summary>
        /// Whether the operator also considers equality
        /// </summary>
        private Boolean andEquals;

        /// <summary>
        /// Creates an InequalityOperator.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule.</param>
        /// <param name="isNegative">Whether the operator compares by greater than or by less than.</param>
        /// <param name="andEquals">Whether the operator also considers equality.</param>
        public InequalityOperator(JToken specifiedValue, bool isNegative, bool andEquals)
        {
            this.SpecifiedValue = specifiedValue ?? throw new ArgumentNullException(nameof(specifiedValue));
            this.IsNegative = isNegative;
            this.andEquals = andEquals;
        }

        /// <summary>
        /// Evaluates <paramref name="tokenToEvaluate"/> to determine if it is greater, greaterOrEquals, less, or lessOrEquals than the specified value.
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

            // TODO before this catch wrong types, like float vs date

            var normalizedSpecifiedValue = GetNormalizedValue(this.SpecifiedValue);
            var normalizedTokenToEvaluate = GetNormalizedValue(tokenToEvaluate);

            var result = normalizedSpecifiedValue > normalizedTokenToEvaluate;

            if (this.IsNegative)
            {
                result = !result;
            }

            if (this.andEquals)
            {
                result = result || normalizedSpecifiedValue == normalizedTokenToEvaluate;

            }

            return result;
        }

        private double GetNormalizedValue(JToken token)
        {
            double value;

            if (token.Type == JTokenType.Date)
            {
                value = token.Value<DateTime>().ToOADate();
            } else
            {
                // TODO catch type errors earlier?
                value = token.Value<double>();
            }

            return value;
        }

        private string GetName() {
            if (IsNegative && andEquals)
            {
                return "LessOrEquals";
            }
            else if (IsNegative && !andEquals)
            {
                return "Less";
            }
            else if (!IsNegative && andEquals)
            {
                return "GreaterOrEquals";
            }
            else
            {
                return "Greater";
            }
        }
    }
}