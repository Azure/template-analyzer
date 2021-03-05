// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Represents a leaf expression in a JSON rule.
    /// </summary>
    internal class LeafExpression : Expression
    {
        /// <summary>
        /// Creates a LeafExpression.
        /// </summary>
        /// <param name="resourceType">The resource type this expression evaluates.</param>
        /// <param name="path">The JSON path being evaluated.</param>
        /// <param name="operator">The operator used to evaluate the resource type and/or path.</param>
        public LeafExpression(string resourceType, string path, LeafExpressionOperator @operator)
        {
            this.ResourceType = resourceType;
            this.Path = path ?? throw new ArgumentNullException(nameof(path));
            this.Operator = @operator ?? throw new ArgumentNullException(nameof(@operator));
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
        /// <returns>A <see cref="JsonRuleEvaluation"/> with zero or more results of the evaluation, depending on whether there are any/multiple resources of the given type,
        /// and if the path contains any wildcards.</returns>
        public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
        {
            if (jsonScope == null)
            {
                throw new ArgumentNullException(nameof(jsonScope));
            }

            List<IJsonPathResolver> scopesToEvaluate = new List<IJsonPathResolver>();

            if (!string.IsNullOrEmpty(ResourceType))
            {
                scopesToEvaluate.AddRange(jsonScope.ResolveResourceType(ResourceType));
            }
            else
            {
                scopesToEvaluate.Add(jsonScope);
            }

            List<JsonRuleResult> jsonRuleResults = new List<JsonRuleResult>();
            bool evaluationPassed = true;

            foreach (var scope in scopesToEvaluate)
            {
                var leafScope = scope?.Resolve(Path);

                foreach (var propertyToEvaluate in leafScope)
                {
                    bool passed = Operator.EvaluateExpression(propertyToEvaluate.JToken);
                    evaluationPassed &= passed;

                    jsonRuleResults.Add(new JsonRuleResult
                    {
                        Passed = passed,
                        JsonPath = propertyToEvaluate.Path,
                        Expression = this
                    });
                }
            }

            return new JsonRuleEvaluation(this, evaluationPassed, jsonRuleResults);
        }
    }
}
