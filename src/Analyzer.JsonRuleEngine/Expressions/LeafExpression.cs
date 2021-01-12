// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine
{
    /// <summary>
    /// Represents a leaf expression in a JSON rule.
    /// </summary>
    internal class LeafExpression : Expression
    {
        /// <summary>
        /// Creates a LeafExpression.
        /// </summary>
        /// <param name="rootRule">The parent <c>RuleDefinition</c>.</param>
        /// <param name="resourceType">The resource type this expression evaluates.</param>
        /// <param name="path">The JSON path being evaluated.</param>
        /// <param name="operator">The operator used to evaluate the resource type and/or path.</param>
        public LeafExpression(RuleDefinition rootRule, string resourceType, string path, LeafExpressionOperator @operator)
            : base(rootRule)
        {
            (this.ResourceType, this.Path, this.Operator) = (resourceType, path, @operator ?? throw new ArgumentNullException(nameof(@operator)));
        }

        /// <summary>
        /// Gets the type of resource to evaluate.
        /// </summary>
        public string ResourceType { get; private set; }

        /// <summary>
        /// Gets the JSON path to evaluate.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the leaf operator evaluating the resource type and/or path of this expression.
        /// </summary>
        public LeafExpressionOperator Operator { get; private set; }

        /// <summary>
        /// Evaluates this leaf expression's resource type and/or path, starting at the specified json scope, with the contained <c>LeafExpressionOperator</c>.
        /// </summary>
        /// <param name="jsonScope">The json to evaluate.</param>
        /// <returns>Zero or more results of the evaluation, depending on whether there are any/multiple resources of the given type,
        /// and if the path contains any wildcards.</returns>
        public override IEnumerable<JsonRuleResult> Evaluate(IJsonPathResolver jsonScope)
        {
            List<JsonRuleResult> results = new List<JsonRuleResult>();

            if (!string.IsNullOrEmpty(ResourceType))
            {
                var resourceResolver = new ResourceResolver(ResourceType, jsonScope.JToken);

                foreach (var resource in resourceResolver.Resources)
                {
                    var resolver = new JsonPathResolver(resource, resource.Path);

                    results.AddRange(EvaluateScope(resolver));
                }
            }
            else
            {
                results.AddRange(EvaluateScope(jsonScope));
            }

            return results;
        }

        private IEnumerable<JsonRuleResult> EvaluateScope(IJsonPathResolver jsonPathResolver)
        {
            if (jsonPathResolver == null)
            {
                throw new ArgumentNullException(nameof(jsonPathResolver));
            }

            var leafScope = jsonPathResolver?.Resolve(Path);

            foreach (var propertyToEvaluate in leafScope)
            {
                yield return new JsonRuleResult
                {
                    RuleDefinition = this.Rule,
                    Passed = Operator.EvaluateExpression(propertyToEvaluate.JToken)
                };
            }
        }
    }
}
