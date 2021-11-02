// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// enumeration of report types supported
    /// </summary>
    public enum ReportFormat
    {
        /// <summary>
        /// output to console
        /// </summary>
        Console = 0,

        /// <summary>
        /// output to sarif report
        /// </summary>
        Sarif
    }
}
