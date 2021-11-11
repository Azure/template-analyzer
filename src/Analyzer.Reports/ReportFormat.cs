// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// Enumeration of report types supported
    /// </summary>
    public enum ReportFormat
    {
        /// <summary>
        /// Output to console
        /// </summary>
        Console = 0,

        /// <summary>
        /// Output to file in SARIF format
        /// </summary>
        Sarif
    }
}
