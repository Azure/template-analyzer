// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An interface used for resolving line numbers in a document.
    /// </summary>
    public interface ISourceLocationResolver
    {
        // TODO: include source file for Bicep and rename to ISourceLocationResolver once nested templates are supported

        /// <summary>
        /// Find the line number of a path.
        /// </summary>
        /// <param name="path">The path describing the location in the document.</param>
        /// <returns>A tuple with the source file and the line number of the specified path.</returns>
        public SourceLocation ResolveSourceLocation(string path);
    }

    /// <summary>
    /// Represents a location in a source file
    /// </summary>
    public struct SourceLocation
    {
        /// <summary>
        /// 
        /// </summary>
        public string FilePath;
        /// <summary>
        /// 
        /// </summary>
        public int LineNumber;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lineNumber"></param>
        public SourceLocation(int lineNumber, string filePath = default)
        {
            this.FilePath = filePath;
            this.LineNumber = lineNumber;
        }
    }
}