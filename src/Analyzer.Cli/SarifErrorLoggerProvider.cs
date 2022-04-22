// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis.Sarif.Writers;
using Microsoft.Extensions.Logging;

// TODO move to another project/folder?

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// TODO 
    /// </summary>
    public class SarifErrorLoggerProvider : ILoggerProvider // TODO rename
    {
        /// <summary>
        /// TODO 
        /// </summary>
        private readonly SarifLogger sarifLogger;

        /// <summary>
        /// TODO 
        /// </summary>
        public SarifErrorLoggerProvider(SarifLogger sarifLogger)
        {
            this.sarifLogger = sarifLogger;
        }

        /// <summary>
        /// TODO 
        /// </summary>
        public ILogger CreateLogger(string categoryName)
        {
            return new SarifErrorLogger(sarifLogger);
        }

        /// <summary>
        /// TODO 
        /// </summary>
        public void Dispose()
        {
        }
    }
}