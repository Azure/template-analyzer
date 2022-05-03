// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class that creates a logger to print a summary of the execution logs
    /// </summary>
    public class SummaryLoggerProvider : ILoggerProvider
    {
        private readonly Dictionary<string, int> loggedErrors;
        private readonly Dictionary<string, int> loggedWarnings;

        /// <summary>
        /// Constructor of the ErrorSummaryLoggerProvider class
        /// </summary>
        public SummaryLoggerProvider(Dictionary<string, int> loggedErrors, Dictionary<string, int> loggedWarnings)
        {
            this.loggedErrors = loggedErrors;
            this.loggedWarnings = loggedWarnings;
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return new SummaryLogger(loggedErrors, loggedWarnings);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}