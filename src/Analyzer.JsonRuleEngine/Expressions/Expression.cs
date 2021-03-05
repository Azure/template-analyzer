// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// The base class for all Expressions in JSON rules.
    /// </summary>
    internal abstract class Expression
    {
        /// <summary>
        /// Executes this <c>Expression</c> against a template.
        /// </summary>
        /// <param name="jsonScope">The specific scope to evaluate.</param>
        /// <returns>An <c>Evaluation</c> with the results.</returns>
        public abstract JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope);
    }
}
