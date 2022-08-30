// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Represents a location in a source file
    /// </summary>
    public class SourceLocation
    {
        /// <summary>
        /// TODO: Null if entrypoint?
        /// </summary>
        public string FilePath;

        /// <summary>
        /// TODO
        /// </summary>
        public int LineNumber;

        /// <summary>
        /// The source location where the current line is referencing (i.e. bicep module)
        /// </summary>
        public SourceLocation ReferencedLocation;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lineNumber"></param>
        /// <param name="referencedLocation"></param>
        public SourceLocation(int lineNumber, string filePath = default, SourceLocation referencedLocation = null)
        {
            this.FilePath = filePath;
            this.LineNumber = lineNumber;
            this.ReferencedLocation = referencedLocation;
        }

        /// <summary>
        /// Returns the actual source location, not a reference location (i.e. module)
        /// </summary>
        /// <returns></returns>
        public SourceLocation GetActualLocation()
        {
            SourceLocation curLocation = this;

            while (curLocation.ReferencedLocation == null)
            {
                curLocation = curLocation.ReferencedLocation;
            }

            return curLocation;
        }
    }
}
