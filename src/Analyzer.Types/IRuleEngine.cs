// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
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
        /// <param name="logger">A logger to report errors and debug information</param>
        /// <returns>The <see cref="IEvaluation"/>s of this engine's rules against the template.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(TemplateContext templateContext, ILogger logger);
    }
}
