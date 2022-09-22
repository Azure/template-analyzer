// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An interface used for resolving line numbers in a document.
    /// </summary>
    public interface ISourceLocationResolver
    {
        /// <summary>
        /// Find the source location of a path.
        /// </summary>
        /// <param name="path">The path describing the location in the document.</param>
        /// <returns>source location to path</returns>
        public SourceLocation ResolveSourceLocation(string path);
    }
}