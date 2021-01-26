// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine.Schema
{
    /// <summary>
    /// The schema for allOf expressions in JSON rules.
    /// </summary>
    internal class AllOfExpressionDefinition : ExpressionDefinition
    {
        /// <summary>
        /// Gets or sets the expressions found in AllOf.
        /// </summary>
        [JsonProperty]
        public ExpressionDefinition[] AllOf { get; set; }

        /// <summary>
        /// Creates a <see cref="AllOfExpression"/> capable of evaluating JSON using the expressions specified in the JSON rule.
        /// </summary>
        /// <returns>The AllOfExpression.</returns>
        public override Expression ToExpression() => new AllOfExpression(ToAllOfExpression().ToArray(), this.ResourceType);

        private IEnumerable<Expression> ToAllOfExpression()
        {
            foreach (var expressionDefinition in this.AllOf)
            {
                yield return expressionDefinition?.ToExpression();
            }
        }
    }
}
