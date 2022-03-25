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
        /// Analyzes a template using the rules this <see cref="IRuleEngine"/> supports.
        /// </summary>
        /// <param name="templateContext">The template context to analyze.</param>
        /// <returns>The <see cref="IEvaluation"/>s of this engine's rules against the template.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(TemplateContext templateContext);

        /// <summary>
        /// Modifies the rules to run based on values defined in the configurations file.
        /// </summary>
        /// <param name="configuration">The configuration specifying rule modifications.</param>
        public void FilterRules(string configuration);
    }
}
