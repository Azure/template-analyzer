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
        /// Error: There was an error with the command.
        /// This is returned by rootCommand.InvokeAsync in the CLI if there is an error.
        /// </summary>
        ErrorCommand = 1,

        /// <summary>
        /// Error: Invalid file or directory path
        /// </summary>
        ErrorInvalidPath = 2,

        /// <summary>
        /// Error: Missing file or directory path
        /// </summary>
        ErrorMissingPath = 3,

        /// <summary>
        /// Error: Problem loading configuration
        /// </summary>
        ErrorInvalidConfiguration = 4,

        /// <summary>
        /// Error: Invalid ARM template
        /// </summary>
        ErrorInvalidARMTemplate = 10,

        /// <summary>
        /// Error: Invalid Bicep template
        /// </summary>
        ErrorInvalidBicepTemplate = 11,

        /// <summary>
        /// Violation: Scan found rule violations
        /// </summary>
        Violation = 20,

        /// <summary>
        /// Error: There was an error analyzing a template
        /// </summary>
        ErrorAnalysis = 21,

        /// <summary>
        /// Error + Violation: Scan has both rule violations and analysis errors
        /// </summary>
        ErrorAndViolation = 22,
    };
}
