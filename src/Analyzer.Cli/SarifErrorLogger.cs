// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.Extensions.Logging;

// TODO move to another project/folder?

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// TODO 
    /// </summary>
    public class SarifErrorLogger : ILogger // TODO rename
    {
        /// <summary>
        /// TODO 
        /// </summary>
        private readonly SarifLogger sarifLogger;

        /// <summary>
        /// TODO 
        /// </summary>
        public SarifErrorLogger(SarifLogger sarifLogger)
        {
            this.sarifLogger = sarifLogger;
        }

        /// <summary>
        /// TODO 
        /// </summary>
        public IDisposable BeginScope<TState>(TState state) => default!;

        /// <summary>
        /// TODO 
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == LogLevel.Error || logLevel == LogLevel.Warning;
        }

        /// <summary>
        /// TODO 
        /// </summary>
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
                Message = new Message { Text = notificationMessage }, // FIXME
                Level = failureLevel
            };

            sarifLogger.LogToolNotification(notification);  
        }
    }
}