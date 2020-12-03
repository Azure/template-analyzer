// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Armory.JsonRuleEngine
{
    internal class HasValueOperator : LeafExpressionOperator
    {
        public override string Name => "HasValue";

        /// <summary>
        /// Gets the effective value this operator will compare against
        /// </summary>
        public bool EffectiveValue { get; private set; }

        public HasValueOperator(bool specifiedValue, bool isNegative)
        {
            (this.SpecifiedValue, this.IsNegative) = (specifiedValue, isNegative);

            this.EffectiveValue = specifiedValue;
        }
    }
}
