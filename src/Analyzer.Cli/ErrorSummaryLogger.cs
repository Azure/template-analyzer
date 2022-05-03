// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class to print a summary of execution errors and warnings
    /// </summary>
    public class ErrorSummaryLogger : ILogger
    {
        private readonly Dictionary<string, int> loggedErrors;
        private readonly Dictionary<string, int> loggedWarnings;

        /// <summary>
        /// Constructor of the ErrorSummaryLogger class
        /// </summary>
        public ErrorSummaryLogger(Dictionary<string, int> loggedErrors, Dictionary<string, int> loggedWarnings)
        {
            this.loggedErrors = loggedErrors;
            this.loggedWarnings = loggedWarnings;
        }

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) => default!;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var logMessage = state.ToString();

            if (logLevel == LogLevel.Error && !loggedErrors.TryAdd(logMessage, 1))
            {
                loggedErrors[logMessage] += 1;
            }

            if (logLevel == LogLevel.Warning && !loggedWarnings.TryAdd(logMessage, 1))
            {
                loggedWarnings[logMessage] += 1;
            }
        }
    }
}