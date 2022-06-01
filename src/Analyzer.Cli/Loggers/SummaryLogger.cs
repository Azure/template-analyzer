// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Class to print a summary of the execution logs to the standard output
    /// </summary>
    public class SummaryLogger : ILogger
    {
        private readonly Regex omitFromSummaryRegex = new(@"^Unable to analyze \d+ files?:", RegexOptions.Compiled);

        private readonly Dictionary<string, int> loggedErrors = new();
        private readonly Dictionary<string, int> loggedWarnings = new();
        private readonly bool verbose;

        /// <summary>
        /// Constructor of the SummaryLogger class
        /// </summary>
        public SummaryLogger(bool verbose)
        {
            this.verbose = verbose;
        }

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) => default!;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var recordLog = new Action<Dictionary<string, int>, string>((logs, newLogMessage) => {
                if (!logs.TryAdd(newLogMessage, 1))
                {
                    logs[newLogMessage] += 1;
                }
            });

            var logMessage = formatter(state, exception);

            if (omitFromSummaryRegex.IsMatch(logMessage))
                return;

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
        public void SummarizeLogs()
        {
            // Local function to describe a count by returning the number with the appropriate form
            // (singular or plural - just adding 's') of its description
            string DescNum(int count, string description) => $"{count} {description}{(count == 1 ? "" : "s")}";

            Console.WriteLine($"{Environment.NewLine}Analysis output:");
            if (loggedErrors.Count > 0 || loggedWarnings.Count > 0)
            {                
                if (!this.verbose)
                {
                    Console.WriteLine("\tThe verbose mode (option -v or --verbose) can be used to obtain even more information about the execution.");
                }

                void PrintSummary(Dictionary<string, int> logs, string description)
                {
                    if (logs.Count > 0)
                    {
                        Console.WriteLine($"{Environment.NewLine}\tSummary of the {description}:");

                        foreach (KeyValuePair<string, int> log in logs)
                        {
                            Console.WriteLine($"\t\t{DescNum(log.Value, "instance")} of: {log.Key}");
                        }
                    }
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                PrintSummary(loggedWarnings, "warnings");

                Console.ForegroundColor = ConsoleColor.Red;
                PrintSummary(loggedErrors, "errors");
                Console.ResetColor();

                if (loggedWarnings.Count > 0)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{Environment.NewLine}\t{DescNum(loggedWarnings.Values.Sum(), "Warning")}");
                Console.ResetColor();

                if (loggedErrors.Count > 0)
                    Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\t{DescNum(loggedErrors.Values.Sum(), "Error")}");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"\tAnalysis completed successfully");
            }
        }
    }
}