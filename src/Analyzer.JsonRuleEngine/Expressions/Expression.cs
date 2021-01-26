// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine
{
    /// <summary>
    /// The base class for all Expressions in JSON rules.
    /// </summary>
    internal abstract class Expression
    {
        /// <summary>
        /// Gets the type of resource to evaluate.
        /// </summary>
        public string ResourceType { get; private set; }

        /// <summary>
        /// Gets the JSON path to evaluate.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets the Where condition of this expression.
        /// </summary>
        internal Expression Where { get; private set; }

        /// <summary>
        /// Initialization for the base Expression.
        /// </summary>
        /// <param name="resourceType">The resource type this expression evaluates.</param>
        /// <param name="path">The JSON path being evaluated.</param>
        /// <param name="where">The Where condition of this expression.</param>
        internal Expression(string resourceType, string path, Expression where)
        {
            this.ResourceType = resourceType;
            this.Path = path;
            this.Where = where;
        }

        /// <summary>
        /// Executes this <c>Expression</c> against a template.
        /// </summary>
        /// <param name="jsonScope">The specific scope to evaluate.</param>
        /// <returns>The results of the evaluation.</returns>
        public IEnumerable<JsonRuleResult> Evaluate(IJsonPathResolver jsonScope)
        {
            if (jsonScope == null)
            {
                throw new ArgumentNullException(nameof(jsonScope));
            }

            IEnumerable<IJsonPathResolver> scopesToEvaluate;
            if (!string.IsNullOrEmpty(ResourceType))
            {
                scopesToEvaluate= jsonScope.ResolveResourceType(ResourceType);
            }
            else
            {
                scopesToEvaluate = new[] { jsonScope };
            }

            foreach (var initialScope in scopesToEvaluate)
            {
                IEnumerable<IJsonPathResolver> expandedScopes = Path == null ?
                    new[] { initialScope }.AsEnumerable() :
                    initialScope?.Resolve(Path);

                foreach (var propertyToEvaluate in expandedScopes)
                {
                    if (Where != null)
                    {
                        // Needs to return list of Evaluations
                        // that each contain the scope they evaluated.
                        Where.Evaluate(propertyToEvaluate);

                        // foreach scopeWherePassed:
                        // yield return EvaluateInternal(scopeWherePassed);
                    }
                    else
                    {
                        yield return EvaluateInternal(propertyToEvaluate);
                    }
                }
            }
        }

        /// <summary>
        /// Performs implementation-specific evaluation against the JSON scope.
        /// </summary>
        /// <param name="jsonScope">The specific scope to evaluate.</param>
        /// <returns>The results of the evaluation.</returns>
        protected abstract JsonRuleResult EvaluateInternal(IJsonPathResolver jsonScope);
    }
}
