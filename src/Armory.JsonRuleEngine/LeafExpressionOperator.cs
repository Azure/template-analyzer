// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine
{
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
        /// Gets or sets the value specified in the original JSON
        /// </summary>
        public JToken SpecifiedValue { get; set; }
    }
}
