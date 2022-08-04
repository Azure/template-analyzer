// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An interface used for resolving line numbers in a document.
    /// </summary>
    public interface ILineNumberResolver
    {
        // TODO: include source file for Bicep and rename to ISourceLocationResolver once nested templates are supported

        /// <summary>
        /// Find the line number of a path.
        /// </summary>
        /// <param name="path">The path describing the location in the document.</param>
        /// <returns>The line number of the specified path.</returns>
        public int ResolveLineNumber(string path);
    }
}