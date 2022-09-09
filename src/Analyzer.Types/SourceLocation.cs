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
        /// The file path of the source file
        /// </summary>
        public readonly string FilePath;

        /// <summary>
        /// The line number in the soruce file
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// The source location where the current location is referencing (i.e. line for bicep module)
        /// </summary>
        public readonly SourceLocation ReferencedLocation;

        // <summary>
        // TODO 
        // </summary>
        //public readonly SourceLocation ActualLocation;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lineNumber"></param>
        /// <param name="referencedLocation"></param>
        public SourceLocation(string filePath, int lineNumber, SourceLocation referencedLocation = null)
        {
            this.FilePath = filePath;
            this.LineNumber = lineNumber;
            this.ReferencedLocation = referencedLocation;
            //this.ActualLocation = this.GetActualLocation();
        }

        /// <summary>
        /// Returns the actual source location, not a reference location (i.e. module)
        /// </summary>
        /// <returns></returns>
        public SourceLocation GetActualLocation()
        {
            SourceLocation curLocation = this;

            while (curLocation.ReferencedLocation != null)
            {
                curLocation = curLocation.ReferencedLocation;
            }

            return curLocation;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return FilePath.GetHashCode() ^ LineNumber.GetHashCode();
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var result = obj as SourceLocation;
            return (result != null) && Equals(result);
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(SourceLocation other)
        {
            var thisActual = this.GetActualLocation();
            var otherActual = other.GetActualLocation();

            return thisActual.FilePath.Equals(otherActual.FilePath)
                && thisActual.LineNumber.Equals(otherActual.LineNumber);
        }
    }
}
