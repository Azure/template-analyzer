// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    internal class EqualsOperator : LeafExpressionOperator
    {
        public override string Name => this.IsNegative ? "NotEquals" : "Equals";

        public EqualsOperator(JToken specifiedValue, bool isNegative)
        {
            this.SpecifiedValue = specifiedValue;
            this.IsNegative = isNegative;
        }

        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            var normalizedSpecifiedValue = NormalizeValue(this.SpecifiedValue);
            var normalizedTokenToEvaluate = NormalizeValue(tokenToEvaluate);

            return this.IsNegative != JToken.DeepEquals(normalizedSpecifiedValue, normalizedTokenToEvaluate);
        }

        private JToken NormalizeValue(JToken token)
        {
            return token.Type switch
            {
                JTokenType.Integer => JToken.FromObject(token.Value<float>()),
                JTokenType.String => JToken.FromObject(token.Value<string>().ToLower()),
                _ => token,
            };
        }

    }
}