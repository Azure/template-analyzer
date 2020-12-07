// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine
{
    /// <summary>
    /// The schema for leaf expressions in JSON rules.
    /// </summary>
    internal class LeafExpressionDefinition : ExpressionDefinition
    {
        /// <summary>
        /// Gets or sets the Exists property
        /// </summary>
        [JsonProperty]
        public bool? Exists { get; set; }

        /// <summary>
        /// Gets or sets the HasValue property
        /// </summary>
        [JsonProperty]
        public bool? HasValue { get; set; }

        /// <summary>
        /// Gets or sets the Equals property
        /// </summary>
        [JsonProperty(PropertyName = "equals")]
        public JToken Is { get; set; }

        /// <summary>
        /// Gets or sets the NotEquals property
        /// </summary>
        [JsonProperty]
        public JToken NotEquals { get; set; }

        /// <summary>
        /// Gets or sets the Regex property
        /// </summary>
        [JsonProperty]
        public string Regex { get; set; }

        /// <summary>
        /// Gets or sets the In property
        /// </summary>
        [JsonProperty]
        public JToken In { get; set; }

        /// <summary>
        /// Gets or sets the Less property
        /// </summary>
        [JsonProperty]
        public JToken Less { get; set; }

        /// <summary>
        /// Gets or sets the LessOrEqual property
        /// </summary>
        [JsonProperty]
        public JToken LessOrEqual { get; set; }

        /// <summary>
        /// Gets or sets the Greater property
        /// </summary>
        [JsonProperty]
        public JToken Greater { get; set; }

        /// <summary>
        /// Gets or sets the GreaterOrEqual property
        /// </summary>
        [JsonProperty]
        public JToken GreaterOrEqual { get; set; }

        /// <summary>
        /// Creates a <c>LeafExpression</c> capable of evaluating JSON using the operator specified in the JSON rule.
        /// </summary>
        /// <param name="rootRule">The JSON rule this leaf expression is part of.</param>
        /// <returns>The LeafExpression.</returns>
        public override Expression ToExpression(RuleDefinition rootRule)
        {
            LeafExpressionOperator leafOperator = null;

            if (this.Exists != null)
            {
                leafOperator = new ExistsOperator(Exists.Value, isNegative: false);
            }
            else if (this.HasValue != null)
            {
                leafOperator = new HasValueOperator(HasValue.Value, isNegative: false);
            }

            if (leafOperator != null)
            {
                return new LeafExpression(rootRule, this.ResourceType, this.Path, leafOperator);
            }

            throw new NotImplementedException();
        }
    }
}
