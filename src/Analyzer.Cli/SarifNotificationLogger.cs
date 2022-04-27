// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class to output logs as tool notifications in the SARIF file
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
            return true;
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var failureLevel = logLevel switch
            {
                LogLevel.Error => FailureLevel.Error,
                LogLevel.Warning => FailureLevel.Warning,
                _ => FailureLevel.Note,
            };

            var notification = new Notification
            {
                Message = new Message { Text = state.ToString() },
                Level = failureLevel
            };

            if (exception != null)
            {
                notification.Exception = ExceptionData.Create(exception);
            }

            sarifLogger.LogToolNotification(notification);  
        }
    }
}