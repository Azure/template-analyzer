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
        public readonly SourceLocation ReferencedBy;

        // <summary>
        // TODO 
        // </summary>
        //public readonly SourceLocation ActualLocation;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lineNumber"></param>
        /// <param name="referencedBy"></param>
        public SourceLocation(string filePath, int lineNumber, SourceLocation referencedBy = null)
        {
            this.FilePath = filePath;
            this.LineNumber = lineNumber;
            this.ReferencedBy = referencedBy;
            //this.ActualLocation = this.GetActualLocation();
        }

        ///// <summary>
        ///// Returns the actual source location, not a reference location (i.e. module)
        ///// </summary>
        ///// <returns></returns>
        //public SourceLocation GetActualLocation()
        //{
        //    SourceLocation curLocation = this;

        //    while (curLocation.ReferencedBy != null)
        //    {
        //        curLocation = curLocation.ReferencedBy;
        //    }

        //    return curLocation;
        //}

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
            return this.FilePath.Equals(other.FilePath)
                && this.LineNumber.Equals(other.LineNumber);
        }
    }
}
