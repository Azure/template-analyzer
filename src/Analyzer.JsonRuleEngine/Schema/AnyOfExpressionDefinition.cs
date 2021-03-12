// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
{
    /// <summary>
    /// The schema for anyOf expressions in JSON rules.
    /// </summary>
    internal class AnyOfExpressionDefinition : ExpressionDefinition
    {
        /// <summary>
        /// Gets or sets the expressions found in AnyOf.
        /// </summary>
        [JsonProperty]
        public ExpressionDefinition[] AnyOf { get; set; }

        /// <summary>
        /// Creates a <see cref="AnyOfExpression"/> capable of evaluating JSON using the expressions specified in the JSON rule.
        /// </summary>
        /// <returns>The AnyOfExpression.</returns>
        public override Expression ToExpression() => new AnyOfExpression(ToAnyOfExpression().ToArray(), path: this.Path, resourceType: this.ResourceType);

        private IEnumerable<Expression> ToAnyOfExpression()
        {
            foreach (var expressionDefinition in this.AnyOf)
            {
                yield return expressionDefinition?.ToExpression();
            }
        }
    }
}
