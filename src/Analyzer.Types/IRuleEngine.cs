// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Interface for the TemplateAnalyzer rule engine
    /// </summary>
    public interface IRuleEngine
    {
        /// <summary>
        /// Evaluates a template using the specified rules.
        /// </summary>
        /// <param name="templateContext">The template context to evaluate.</param>
        /// <returns>The evaluations of the rules against the template.</returns>
        public IEnumerable<IEvaluation> EvaluateTemplate(TemplateContext templateContext);
    }
}
