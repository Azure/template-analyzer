// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Armory.JsonRuleEngine
{
    internal class LeafExpression : Expression
    {
        /// <summary>
        /// Constructor for LeafExpression
        /// </summary>
        /// <param name="resourceType">The resource type this expression evaluates</param>
        /// <param name="path">The JSON path being evaluated</param>
        /// <param name="operator">The operator used to evaluate the resource type and/or path</param>
        public LeafExpression(string resourceType, string path, LeafExpressionOperator @operator)
        {
            (this.ResourceType, this.Path, this.Operator) = (resourceType, path, @operator);
        }

        /// <summary>
        /// Gets the type of resource to evaluate
        /// </summary>
        public string ResourceType { get; private set; }

        /// <summary>
        /// Gets the JSON path to evaluate
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the leaf operator evaluating the resource type and/or path of this expression
        /// </summary>
        public LeafExpressionOperator Operator { get; private set; }
    }
}
