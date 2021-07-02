// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// An operator that evaluates the "greater", "greaterOrEquals", "less", and "lessOrEquals" JSON expressions.
    /// </summary>
    internal class InequalityOperator : LeafExpressionOperator
    {
        /// <inheritdoc/>
        public override string Name =>
            Greater && OrEquals ? "GreaterOrEquals" :
            Greater && !OrEquals ? "Greater" :
            !Greater && OrEquals ? "LessOrEquals" :
            "Less";

        /// <summary>
        /// Whether the operator compares by greater than or by less than.
        /// </summary>
        public bool Greater { get; private set; }

        /// <summary>
        /// Whether the operator also considers equality.
        /// </summary>
        public bool OrEquals { get; private set; }

        /// <summary>
        /// Gets the effective value this operator will compare against.
        /// </summary>
        public double EffectiveValue { get; private set; }

        /// <summary>
        /// Creates an InequalityOperator.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule.</param>
        /// <param name="greater">Whether the operator compares by greater than or by less than.</param>
        /// <param name="orEquals">Whether the operator also considers equality.</param>
        public InequalityOperator(JToken specifiedValue, bool greater, bool orEquals)
        {
            this.SpecifiedValue = specifiedValue ?? throw new ArgumentNullException(nameof(specifiedValue));
            this.EffectiveValue = GetFinalComparisonTermIfValid(specifiedValue) ?? throw new InvalidOperationException($"Cannot compare against a {specifiedValue.Type} using an InequalityOperator");
            this.Greater = greater;
            this.OrEquals = orEquals;
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
                return false; // Not ideal, will be improved in the future
            }

            var finalTokenToEvaluate = GetFinalComparisonTermIfValid(tokenToEvaluate);

            if (finalTokenToEvaluate == null ||
                (SpecifiedValue.Type == JTokenType.String && tokenToEvaluate.Type != JTokenType.String) ||
                (tokenToEvaluate.Type == JTokenType.String && SpecifiedValue.Type != JTokenType.String))
            {
                return false; // Not ideal, will be improved in the future
            }

            var result = Greater ? EffectiveValue > finalTokenToEvaluate : EffectiveValue < finalTokenToEvaluate;

            if (OrEquals)
            {
                result = result || EffectiveValue == finalTokenToEvaluate;
            }

            return result;
        }

        private double? GetFinalComparisonTermIfValid(JToken term)
        {
            if (term.Type == JTokenType.String)
            {
                try
                {
                    return DateTime.Parse(term.Value<string>(), styles: DateTimeStyles.RoundtripKind).ToOADate();
                }
                catch
                {
                    return null;
                }
            }
            else if (term.Type == JTokenType.Float || term.Type == JTokenType.Integer)
            {
                return term.Value<double>();
            }

            return null;
        }
    }
}