// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine
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
            =>  this.IsNegative != JToken.DeepEquals(tokenToEvaluate, this.SpecifiedValue);
    }
}