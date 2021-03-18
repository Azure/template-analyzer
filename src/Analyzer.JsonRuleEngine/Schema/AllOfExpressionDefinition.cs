// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
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
        /// <param name="jsonLineNumberResolver">An <c>IJsonLineNumberResolver</c> to
        /// pass to the created <c>Expression</c>.</param>
        /// <returns>The AllOfExpression.</returns>
        public override Expression ToExpression(ILineNumberResolver jsonLineNumberResolver)
            => new AllOfExpression(this.AllOf.Select(e => e.ToExpression(jsonLineNumberResolver)).ToArray(), resourceType: this.ResourceType, path: this.Path);
    }
}
