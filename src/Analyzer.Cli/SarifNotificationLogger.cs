// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class to log warnings and errors as tool notifications in the SARIF output
    /// </summary>
    public class SarifNotificationLogger : ILogger
    {
        /// <summary>
        /// Class used to output information to the SARIF file
        /// </summary>
        private readonly SarifLogger sarifLogger;

        /// <summary>
        /// Constructor of the SarifNotificationLogger class
        /// </summary>
        /// <param name="sarifLogger">Class used to output information to the SARIF file</param>
        public SarifNotificationLogger(SarifLogger sarifLogger)
        {
            this.sarifLogger = sarifLogger;
        }

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) => default!;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Error || logLevel == LogLevel.Warning;
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var notificationMessage = state.ToString();
            if (exception != null)
            {
                Func<Exception, string> getExceptionInfo = (exception) => " - " + exception.Message + " - " + exception.StackTrace;

                while (exception != null)
                {
                    notificationMessage += getExceptionInfo(exception);
                    exception = exception.InnerException;
                }
            }

            var failureLevel = logLevel == LogLevel.Error ? FailureLevel.Error : FailureLevel.Warning;

            var notification = new Notification
            {
                Message = new Message { Text = notificationMessage },
                Level = failureLevel
            };

            sarifLogger.LogToolNotification(notification);  
        }
    }
}