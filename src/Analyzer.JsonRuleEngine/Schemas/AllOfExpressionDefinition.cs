// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
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
        /// <param name="isNegative">Whether to negate the result of the evaluation.</param>
        /// <returns>The AllOfExpression.</returns>
        public override Expression ToExpression(bool isNegative = false)
        {
            if (!isNegative)
            {
                return new AllOfExpression(this.AllOf.Select(e => e.ToExpression(isNegative)).ToArray(), this.CommonProperties);
            }
            else
            {
                // De Morgan's Law
                return new AnyOfExpression(this.AllOf.Select(e => e.ToExpression(isNegative)).ToArray(), this.CommonProperties);
            }
        }

        /// <summary>
        /// Validates the <see cref="AllOfExpressionDefinition"/> for valid syntax
        /// </summary>
        internal override void Validate()
        {
            if (!(this.AllOf?.Count() > 0))
            {
                throw new JsonException("No expressions were specified in the allOf expression");
            }

            int nullCount = this.AllOf.Count(e => e == null);

            if (nullCount > 0)
            {
                throw new JsonException($"Null expressions are not valid. {nullCount} expressions are null in allOf expression");
            }
        }
    }
}
