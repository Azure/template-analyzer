// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// The options for <see cref="ConsoleLoggerFormatter"/>.
    /// </summary>
    public class ConsoleLoggerFormatterOptions : ConsoleFormatterOptions
    {
        /// <summary>
        /// Whether the verbose option is enabled.
        /// </summary>
        public bool Verbose { get; set; }
    }
}