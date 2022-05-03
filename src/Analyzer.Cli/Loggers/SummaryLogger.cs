// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class to print a summary of the execution logs
    /// </summary>
    public class SummaryLogger : ILogger
    {
        private readonly Dictionary<string, int> loggedErrors;
        private readonly Dictionary<string, int> loggedWarnings;

        /// <summary>
        /// Constructor of the SummaryLogger class
        /// </summary>
        public SummaryLogger(Dictionary<string, int> loggedErrors, Dictionary<string, int> loggedWarnings)
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
            var recordLog = new Action<Dictionary<string, int>, string>((logs, newLogMessage) => {
                if (!logs.TryAdd(newLogMessage, 1))
                {
                    logs[newLogMessage] += 1;
                }
            });

            var logMessage = state.ToString();

            if (logLevel == LogLevel.Error)
            {
                recordLog(loggedErrors, logMessage);
            }
            else if (logLevel == LogLevel.Warning)
            {
                recordLog(loggedWarnings, logMessage);
            }
        }
    }
}