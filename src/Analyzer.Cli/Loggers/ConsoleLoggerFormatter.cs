using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// A <see cref="ConsoleLoggerFormatter"/> for the CLI logger.
    /// </summary>
    public sealed class ConsoleLoggerFormatter : ConsoleFormatter
    {
        private const string defaultForegroundColor = "\x1B[39m\x1B[22m";
        private ConsoleLoggerFormatterOptions formatterOptions;
        private readonly IDisposable optionsReloadToken;

        static string GetColorForTextWriter(ConsoleColor color) =>
        color switch
        {
            ConsoleColor.Red => "\x1B[1m\x1B[31m",
            ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
            ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
            _ => defaultForegroundColor
        };

        /// <summary>
        /// Creates an instance of <see cref="ConsoleLoggerFormatter"/>.
        /// </summary>
        /// <param name="options">The formatter options.</param>
        public ConsoleLoggerFormatter(IOptionsMonitor<ConsoleLoggerFormatterOptions> options)
            : base("ConsoleLoggerFormatter") =>
            (this.optionsReloadToken, this.formatterOptions) = (options.OnChange(ReloadLoggerOptions), options.CurrentValue);

        private void ReloadLoggerOptions(ConsoleLoggerFormatterOptions options)
        {
            this.formatterOptions = options;
        }

        /// <inheritdoc/>
        public void Dispose() => optionsReloadToken?.Dispose();

        /// <inheritdoc/>
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);

            if (message is null)
            {
                return;
            }

            if (logEntry.LogLevel == LogLevel.Error)
            {
                textWriter.Write(GetColorForTextWriter(ConsoleColor.Red));
                textWriter.Write("Error: ");
            }
            else if (logEntry.LogLevel == LogLevel.Warning)
            {
                textWriter.Write(GetColorForTextWriter(ConsoleColor.Yellow));
                textWriter.Write("Warning: ");
            }
            else if (logEntry.LogLevel == LogLevel.Debug)
            {
                textWriter.Write(GetColorForTextWriter(ConsoleColor.Cyan));
                textWriter.Write("Debug information: ");
            }

            textWriter.WriteLine(message);

            if (this.formatterOptions.Verbose && logEntry.Exception != null)
            {
                textWriter.WriteLine("Exception details:");
                textWriter.WriteLine(logEntry.Exception.ToString());
            }

            textWriter.Write(defaultForegroundColor);
        }
    }
}