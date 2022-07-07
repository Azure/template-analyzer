// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class that creates a logger to print a summary of the execution logs to the standard output
    /// </summary>
    public class SummaryLoggerProvider : ILoggerProvider
    {
        private SummaryLogger summaryLogger;

        /// <summary>
        /// Constructs a <see cref="SummaryLoggerProvider"/>.
        /// </summary>
        /// <param name="summaryLogger">The <see cref="SummaryLogger"/> used to summarize log entries.</param>
        public SummaryLoggerProvider(SummaryLogger summaryLogger)
        {
            this.summaryLogger = summaryLogger;
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName) => this.summaryLogger;

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}