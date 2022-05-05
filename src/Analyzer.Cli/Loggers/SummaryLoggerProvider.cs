// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class that creates a logger to print a summary of the execution logs
    /// </summary>
    public class SummaryLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// The summary logger
        /// </summary>
        public SummaryLogger SummaryLogger;

        /// <summary>
        /// Constructor of the ErrorSummaryLoggerProvider class
        /// </summary>
        public SummaryLoggerProvider()
        {
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            this.SummaryLogger = new SummaryLogger();

            return this.SummaryLogger;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}