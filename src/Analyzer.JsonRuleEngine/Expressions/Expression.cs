// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
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
        /// Gets the Where condition of this <see cref="Expression"/>.
        /// </summary>
        public Expression Where { get; private set; }

        /// <summary>
        /// Initialization for the base Expression.
        /// </summary>
        /// <param name="commonProperties">The properties common across all <see cref="Expression"/> types.</param>
        internal Expression(ExpressionCommonProperties commonProperties)
        {
            (this.ResourceType, this.Path, this.Where) = (commonProperties?.ResourceType, commonProperties?.Path, commonProperties?.Where);
        }

        /// <summary>
        /// Executes this <see cref="Expression"/> against a template.
        /// </summary>
        /// <param name="jsonScope">The specific scope to evaluate.</param>
        /// <param name="jsonLineNumberResolver">An <see cref="ILineNumberResolver"/> to
        /// map JSON paths in the returned evaluation to the line number in the JSON evaluated.</param>
        /// <returns>An <see cref="IEnumerable{JsonRuleEvaluation}"/> with the results of the evaluation.</returns>
        public abstract IEnumerable<JsonRuleEvaluation> Evaluate(IJsonPathResolver jsonScope, ILineNumberResolver jsonLineNumberResolver);

        /// <summary>
        /// Performs tasks common across <see cref="Expression"/> implementations, such as
        /// determining all paths to run against.
        /// </summary>
        /// <param name="jsonScope">The scope being evaluated.</param>
        /// <param name="doEvaluation">A delegate for logic specific to the child <see cref="Expression"/>, which
        /// generates a <see cref="JsonRuleEvaluation"/> for the specified <paramref name="jsonScope"/>.</param>
        /// <returns>The results of the evaluation.</returns>
        protected IEnumerable<JsonRuleEvaluation> EvaluateInternal(IJsonPathResolver jsonScope, Func<IJsonPathResolver, JsonRuleEvaluation> doEvaluation) =>
            EvaluateInternal(jsonScope, scope => new[] { doEvaluation(scope) });

        /// <summary>
        /// Performs tasks common across <see cref="Expression"/> implementations, such as
        /// determining all paths to run against.
        /// </summary>
        /// <param name="jsonScope">The scope being evaluated.</param>
        /// <param name="doEvaluation">A delegate for logic specific to the child <see cref="Expression"/>, which
        /// generates an enumerable of <see cref="JsonRuleEvaluation"/>s for the specified <paramref name="jsonScope"/>.</param>
        /// <returns>The results of the evaluation.</returns>
        protected IEnumerable<JsonRuleEvaluation> EvaluateInternal(IJsonPathResolver jsonScope, Func<IJsonPathResolver, IEnumerable<JsonRuleEvaluation>> doEvaluation)
        {
            if (jsonScope == null) throw new ArgumentNullException(nameof(jsonScope));

            // Select resources of given type, if specified
            IEnumerable<IJsonPathResolver> scopesToEvaluate;
            if (!string.IsNullOrEmpty(ResourceType))
            {
                scopesToEvaluate = jsonScope.ResolveResourceType(ResourceType);
            }
            else
            {
                scopesToEvaluate = new[] { jsonScope };
            }

            List<JsonRuleEvaluation> evaluations = new List<JsonRuleEvaluation>();

            foreach (var initialScope in scopesToEvaluate)
            {
                // Expand with path if specified
                IEnumerable<IJsonPathResolver> expandedScopes = Path == null ?
                    new[] { initialScope }.AsEnumerable() :
                    initialScope?.Resolve(Path);

                foreach (var propertyToEvaluate in expandedScopes)
                {
                    // Evaluate this path if either (a) there is no Where condition to evaluate, or (b) the Where expression passed for this path.
                    // Do not pass a line number resolver to Where because line numbers in these evaluations do not matter.
                    var whereEvaluation = Where?.Evaluate(propertyToEvaluate, jsonLineNumberResolver: null);
                    if (whereEvaluation == null || whereEvaluation.Any(w => w.Passed && w.HasResults))
                    {
                        // Perform the evaluation on this scope/path
                        evaluations.AddRange(doEvaluation(propertyToEvaluate));
                    }
                }
            }

            return evaluations;
        }
    }
}
