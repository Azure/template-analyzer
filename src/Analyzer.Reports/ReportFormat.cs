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
        /// Output to console
        /// </summary>
        Console = 0,

        /// <summary>
        /// Output to SARIF report
        /// </summary>
        Sarif
    }
}
