// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Armory.JsonRuleEngine
{
    internal class ExistsOperator : LeafExpressionOperator
    {
        public override string Name => "Exists";

        /// <summary>
        /// Gets the effective value this operator will compare against
        /// </summary>
        public bool EffectiveValue { get; private set; }

        public ExistsOperator(bool specifiedValue, bool isNegative)
        {
            (this.SpecifiedValue, this.IsNegative) = (specifiedValue, isNegative);

            this.EffectiveValue = specifiedValue;
        }
    }
}
