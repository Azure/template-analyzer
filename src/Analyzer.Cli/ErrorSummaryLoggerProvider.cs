// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class that creates a logger to print a summary of execution errors and warnings
    /// </summary>
    public class ErrorSummaryLoggerProvider : ILoggerProvider
    {
        private readonly Dictionary<string, int> loggedErrors;
        private readonly Dictionary<string, int> loggedWarnings;

        /// <summary>
        /// Constructor of the ErrorSummaryLoggerProvider class
        /// </summary>
        public ErrorSummaryLoggerProvider(Dictionary<string, int> loggedErrors, Dictionary<string, int> loggedWarnings)
        {
            this.loggedErrors = loggedErrors;
            this.loggedWarnings = loggedWarnings;
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return new ErrorSummaryLogger(loggedErrors, loggedWarnings);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}