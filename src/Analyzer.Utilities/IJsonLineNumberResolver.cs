// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An interface used for resolving line numbers from a processed template to the original template
    /// </summary>
    public interface IJsonLineNumberResolver
    {
        /// <summary>
        /// Given a JSON path in a processed JSON template, find the equivalent line number
        /// in the original JSON template.
        /// </summary>
        /// <param name="pathInProcessedTemplate">The path in the processed template
        /// to find the line number of in the original template.</param>
        /// <param name="processedTemplateRoot">The root of the processed template.</param>
        /// <param name="originalTemplateRoot">The root of the original template.</param>
        /// <returns>The line number of the equivalent location in the original template,
        /// or 0 if it can't be determined.</returns>
        public int ResolveLineNumberForOriginalTemplate(
            string pathInProcessedTemplate,
            JToken processedTemplateRoot,
            JToken originalTemplateRoot);
    }
}