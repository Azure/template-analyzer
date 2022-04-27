// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class that creates a logger to log warnings and errors as tool notifications in the SARIF output
    /// </summary>
    public class SarifNotificationLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Logger used to output information to the SARIF file
        /// </summary>
        private readonly SarifLogger sarifLogger;

        /// <summary>
        /// Constructor of the SarifNotificationLoggerProvider class
        /// </summary>
        /// <param name="sarifLogger">Class used to output information to the SARIF file</param>
        public SarifNotificationLoggerProvider(SarifLogger sarifLogger)
        {
            this.sarifLogger = sarifLogger;
        }

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName)
        {
            return new SarifNotificationLogger(sarifLogger);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}