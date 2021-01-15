// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine
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
        /// <returns>The results of the evaluation.</returns>
        public abstract IEnumerable<JsonRuleResult> Evaluate(IJsonPathResolver jsonScope);
    }
}
