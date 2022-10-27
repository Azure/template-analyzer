// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Represents a location in a source file
    /// </summary>
    public class SourceLocation : IEquatable<SourceLocation>
    {
        /// <summary>
        /// The file path of the source file
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The line number in the soruce file
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Instantiate instance of SourceLocation
        /// </summary>
        /// <param name="filePath">File path of the source location</param>
        /// <param name="lineNumber">Line number of source location</param>
        public SourceLocation(string filePath, int lineNumber)
        {
            this.FilePath = filePath;
            this.LineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return FilePath.GetHashCode() ^ LineNumber.GetHashCode();
        }

        /// <summary>
        /// Compare with object for equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var result = obj as SourceLocation;
            return (result != null) && Equals(result);
        }

        /// <summary>
        /// Compare with source location for equality
        /// </summary>
        /// <param name="sourceLocation"></param>
        /// <returns></returns>
        public bool Equals(SourceLocation sourceLocation)
        {
            return this.FilePath.Equals(sourceLocation.FilePath)
                && this.LineNumber.Equals(sourceLocation.LineNumber);
        }
    }
}
