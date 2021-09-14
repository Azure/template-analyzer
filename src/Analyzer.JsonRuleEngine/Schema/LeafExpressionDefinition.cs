// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
{
    /// <summary>
    /// The schema for leaf expressions in JSON rules.
    /// </summary>
    internal class LeafExpressionDefinition : ExpressionDefinition
    {
        /// <inheritdoc/>
        [JsonProperty(Required = Required.Always)]
        public override string Path { get; set; }

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
        public JArray In { get; set; }

        /// <summary>
        /// Gets or sets the Less property
        /// </summary>
        [JsonProperty]
        public JToken Less { get; set; }

        /// <summary>
        /// Gets or sets the LessOrEquals property
        /// </summary>
        [JsonProperty]
        public JToken LessOrEquals { get; set; }

        /// <summary>
        /// Gets or sets the Greater property
        /// </summary>
        [JsonProperty]
        public JToken Greater { get; set; }

        /// <summary>
        /// Gets or sets the GreaterOrEquals property
        /// </summary>
        [JsonProperty]
        public JToken GreaterOrEquals { get; set; }

        /// <summary>
        /// Creates a <see cref=" LeafExpression"/> capable of evaluating JSON using the operator specified in the JSON rule.
        /// </summary>
        /// <param name="jsonLineNumberResolver">An <see cref="ILineNumberResolver"/> to
        /// pass to the created <see cref="Expression"/>.</param>
        /// <param name="isNegative">Whether to negate the evaluation.</param>
        /// <returns>The LeafExpression.</returns>
        public override Expression ToExpression(ILineNumberResolver jsonLineNumberResolver, bool isNegative = false)
        {
            LeafExpressionOperator leafOperator = null;

            if (this.Exists != null)
            {
                leafOperator = new ExistsOperator(Exists.Value, isNegative);
            }
            else if (this.HasValue != null)
            {
                leafOperator = new HasValueOperator(HasValue.Value, isNegative);
            }
            else if (this.Is != null || this.NotEquals != null)
            {
                leafOperator = new EqualsOperator(
                    specifiedValue: this.Is ?? this.NotEquals,
                    isNegative: this.NotEquals != null ^ isNegative);
            }
            else if (this.Regex != null)
            {
                leafOperator = new RegexOperator(this.Regex, isNegative);
            }
            else if (this.In != null)
            {
                leafOperator = new InOperator(this.In, isNegative);
            }
            else if (this.Greater != null)
            {
                leafOperator = new InequalityOperator(this.Greater, greater: true ^ isNegative, orEquals: false ^ isNegative);
            }
            else if (this.GreaterOrEquals != null)
            {
                leafOperator = new InequalityOperator(this.GreaterOrEquals, greater: true ^ isNegative, orEquals: true ^ isNegative);
            }
            else if (this.Less != null)
            {
                leafOperator = new InequalityOperator(this.Less, greater: false ^ isNegative, orEquals: false ^ isNegative);
            }
            else if (this.LessOrEquals != null)
            {
                leafOperator = new InequalityOperator(this.LessOrEquals, greater: false ^ isNegative, orEquals: true ^ isNegative);
            }

            if (leafOperator != null)
            {
                return new LeafExpression(jsonLineNumberResolver, leafOperator, GetCommonProperties(jsonLineNumberResolver));
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Validates the LeafExpressionDefinition for valid syntax
        /// </summary>
        internal override void Validate()
        {
            // Validation for LeafExpression occurs in ExpressionConverter
        }
    }
}
