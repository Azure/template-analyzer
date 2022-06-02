// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Enumeration of report types supported
    /// </summary>
    public enum ExitCode
    {
        /// <summary>
        /// Success: Operation was successful
        /// </summary>
        Success = 0,

        /// <summary>
        /// Error: GenericError
        /// </summary>
        ErrorGeneric = 1,

        /// <summary>
        /// Error: Invalid file or directory path
        /// </summary>
        ErrorInvalidPath = 2,

        /// <summary>
        /// Error: Missing file or directory path
        /// </summary>
        ErrorMissingPath = 3,

        /// <summary>
        /// Error: Invalid ARM template
        /// </summary>
        ErrorInvalidARMTemplate = 4,

        /// <summary>
        /// Violation: Scan found rule violations
        /// </summary>
        Violation = 5,

        /// <summary>
        /// Error + Violation: Scan has both errors and violations
        /// </summary>
        ErrorAndViolation = 6,

        /// <summary>
        /// Error: Problem loading configuration
        /// </summary>
        ErrorInvalidConfiguration = 7,
    };
}
