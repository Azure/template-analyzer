// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class to print a summary of the execution logs to the standard output
    /// </summary>
    public class SummaryLogger : ILogger
    {
        private readonly Dictionary<string, int> loggedErrors = new();
        private readonly Dictionary<string, int> loggedWarnings = new();

        /// <summary>
        /// Constructor of the SummaryLogger class
        /// </summary>
        public SummaryLogger()
        {
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

        /// <summary>
        /// Outputs to console a summary of the errors and warnings logged
        /// </summary>
        public void SummarizeLogs(bool showVerboseMessage)
        {
            if (loggedErrors.Count > 0 || loggedWarnings.Count > 0)
            {
                Console.ForegroundColor = loggedErrors.Count > 0 ? ConsoleColor.Red : ConsoleColor.Yellow;

                Console.WriteLine($"{Environment.NewLine}{loggedErrors.Count} error(s) and {loggedWarnings.Count} warning(s) were found during the execution, please refer to the original messages above");

                if (!showVerboseMessage)
                {
                    Console.WriteLine("The verbose mode (option -v or --verbose) can be used to obtain even more information about the execution");
                }

                var printSummary = new Action<Dictionary<string, int>, string>((logs, description) => {
                    if (logs.Count > 0)
                    {
                        Console.WriteLine($"Summary of the {description}:");

                        foreach (KeyValuePair<string, int> log in logs)
                        {
                            Console.WriteLine($"\t{log.Value} instance(s) of: {log.Key}");
                        }
                    }
                });

                printSummary(loggedErrors, "errors");

                if (loggedWarnings.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }

                printSummary(loggedWarnings, "warnings");

                Console.ResetColor();
            }
        }
    }
}