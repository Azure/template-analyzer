// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Templates.Analyzer.Core;
using Microsoft.Azure.Templates.Analyzer.Reports;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Creates the command line for running Template Analyzer. 
    /// Instantiates arguments that can be passed and different commands that can be invoked.
    /// </summary>
    internal class CommandLineParser
    {
        private const string DefaultConfigFileName = "configuration.json";

        private RootCommand rootCommand;
        private TemplateAnalyzer templateAnalyzer;

        private IReportWriter reportWriter;
        private ILogger logger;
        private SummaryLogger summaryLogger;

        /// <summary>
        /// Constructor for the command line parser. Sets up the command line API. 
        /// </summary>
        public CommandLineParser()
        {
            SetupCommandLineAPI();
        }

        /// <summary>
        /// Invoke the command line API using the provided arguments. 
        /// </summary>
        /// <param name="args">Arguments sent in via the command line</param>
        /// <returns>A Task that executes the command handler</returns>
        public async Task<int> InvokeCommandLineAPIAsync(string[] args)
        {
            return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private void SetupCommandLineAPI()
        {
            // Command line API is setup using https://github.com/dotnet/command-line-api

            rootCommand = new();
            rootCommand.Description = "Analyze Azure Resource Manager (ARM) and Bicep Templates for security and best practice issues.";

            // Setup analyze-template and analyze-directory commands
            List<Command> allCommands = new()
            {
                SetupAnalyzeTemplateCommand(),
                SetupAnalyzeDirectoryCommand()
            };

            // Add all commands to root command
            allCommands.ForEach(rootCommand.AddCommand);

            // Setup options that apply to all commands
            SetupCommonOptionsForCommands(allCommands);
        }

        private Command SetupAnalyzeTemplateCommand()
        {
            Command analyzeTemplateCommand = new Command(
                "analyze-template",
                "Analyze a single template");

            analyzeTemplateCommand.AddArgument(
                new Argument<FileInfo>(
                    "template-file-path",
                    "The path of the template to analyze"));

            analyzeTemplateCommand.AddOption(
                new Option<FileInfo>(
                    new[] { "-p", "--parameters-file-path" },
                    "The parameter file to use when parsing the specified template")
            );

            // Assign handler method
            analyzeTemplateCommand.Handler = CommandHandler.Create(
                GetType().GetMethod(
                    nameof(AnalyzeTemplateCommandHandler),
                    BindingFlags.Instance | BindingFlags.NonPublic),
                this);

            return analyzeTemplateCommand;
        }

        private Command SetupAnalyzeDirectoryCommand()
        {
            Command analyzeDirectoryCommand = new Command(
                "analyze-directory",
                "Analyze all templates within a directory");

            analyzeDirectoryCommand.AddArgument(
                new Argument<DirectoryInfo>(
                    "directory-path",
                    "The directory to find templates"));

            // Assign handler method
            analyzeDirectoryCommand.Handler = CommandHandler.Create(
                GetType().GetMethod(
                    nameof(AnalyzeDirectoryCommandHandler),
                    BindingFlags.Instance | BindingFlags.NonPublic),
                this);

            return analyzeDirectoryCommand;
        }

        private void SetupCommonOptionsForCommands(List<Command> commands)
        {
            List<Option> options = new()
            {
                new Option<FileInfo>(
                    new[] { "-c", "--config-file-path" },
                    "The configuration file to use when parsing the specified template"),

                new Option<ReportFormat>(
                    "--report-format",
                    "Format of report to be generated"),

                new Option<FileInfo>(
                    new[] { "-o", "--output-file-path" },
                    $"The report file path (required for --report-format {ReportFormat.Sarif})"),

                new Option(
                    new[] { "-v", "--verbose" },
                    "Shows details about the analysis"),

                new Option(
                    "--include-non-security-rules",
                    "Run all the rules against the templates, including non-security rules")
            };

            commands.ForEach(c => options.ForEach(c.AddOption));
        }

        // Note: argument names must match command arguments/options (without "-" characters)
        private int AnalyzeTemplateCommandHandler(
            FileInfo templateFilePath,
            FileInfo parametersFilePath,
            FileInfo configFilePath,
            ReportFormat reportFormat,
            FileInfo outputFilePath,
            bool includeNonSecurityRules,
            bool verbose)
        {
            // Check that template file paths exist
            if (!templateFilePath.Exists)
            {
                Console.Error.WriteLine("Invalid template file path: {0}", templateFilePath);
                return (int)ExitCode.ErrorInvalidPath;
            }

            var setupResult = SetupAnalysis(configFilePath, directoryToAnalyze: null, reportFormat, outputFilePath, includeNonSecurityRules, verbose);
            if (setupResult != ExitCode.Success)
            {
                return (int)setupResult;
            }

            // Verify the file is a valid template
            if (!TemplateDiscovery.IsValidTemplate(templateFilePath))
            {
                logger.LogError("File is not a valid ARM Template. File path: {templateFilePath}", templateFilePath.FullName);
                FinishAnalysis();
                return (int)ExitCode.ErrorInvalidARMTemplate;
            }

            IEnumerable<TemplateAndParams> pairsToAnalyze =
                parametersFilePath != null
                ? new[] { new TemplateAndParams(templateFilePath, parametersFilePath) }
                : TemplateDiscovery.FindParameterFilesForTemplate(templateFilePath);

            var exitCodes = new List<ExitCode>();

            foreach (var templateAndParameters in pairsToAnalyze)
            {
                exitCodes.Add(AnalyzeTemplate(templateAndParameters));
            }

            FinishAnalysis();
            return (int)AnalyzeExitCodes(exitCodes);
        }

        // Note: argument names must match command arguments/options (without "-" characters)
        private int AnalyzeDirectoryCommandHandler(
            DirectoryInfo directoryPath,
            FileInfo configFilePath,
            ReportFormat reportFormat,
            FileInfo outputFilePath,
            bool includeNonSecurityRules,
            bool verbose)
        {
            if (!directoryPath.Exists)
            {
                Console.Error.WriteLine("Invalid directory: {0}", directoryPath);
                return (int)ExitCode.ErrorInvalidPath;
            }

            var setupResult = SetupAnalysis(configFilePath, directoryPath, reportFormat, outputFilePath, includeNonSecurityRules, verbose);
            if (setupResult != ExitCode.Success)
            {
                return (int)setupResult;
            }

            // Find files to analyze
            var filesToAnalyze = TemplateDiscovery.DiscoverTemplatesAndParametersInDirectory(directoryPath, logger);

            // Log root directory info to be analyzed
            Console.WriteLine($"{Environment.NewLine}{Environment.NewLine}Directory: {directoryPath}");

            var exitCodes = new List<ExitCode>();
            foreach (var templateAndParameters in filesToAnalyze)
            {
                exitCodes.Add(AnalyzeTemplate(templateAndParameters));
            }

            int numOfFilesAnalyzed = exitCodes.Where(x => x == ExitCode.Success || x == ExitCode.Violation).Count();
            Console.WriteLine($"{Environment.NewLine}Analyzed {numOfFilesAnalyzed} {(numOfFilesAnalyzed == 1 ? "file" : "files")} in the directory specified.");

            var exitCode = AnalyzeExitCodes(exitCodes);

            FinishAnalysis();

            return (int)exitCode;
        }

        private ExitCode AnalyzeTemplate(TemplateAndParams templateAndParameters)
        {
            try
            {

                (string template, string parameters) = TemplateDiscovery.GetTemplateAndParameterContents(templateAndParameters);

                IEnumerable<IEvaluation> evaluations = this.templateAnalyzer.AnalyzeTemplate(template, templateAndParameters.Template.FullName, parameters);

                this.reportWriter.WriteResults(evaluations, (FileInfoBase)templateAndParameters.Template, (FileInfoBase)templateAndParameters.ParametersFile);

                return evaluations.Any(e => !e.Passed) ? ExitCode.Violation : ExitCode.Success;
            }
            catch (Exception exception)
            {
                // Keeping separate LogError calls so formatting can use the recommended templating.
                // https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2254
                if (templateAndParameters.ParametersFile != null)
                {
                    logger.LogError(exception, "An exception occurred while analyzing template {TemplatePath} with parameters file {ParametersPath}",
                        templateAndParameters.Template.FullName, templateAndParameters.ParametersFile.FullName);
                }
                else
                {
                    logger.LogError(exception, "An exception occurred while analyzing template {TemplatePath}", templateAndParameters.Template.FullName);
                }

                return (exception.Message == TemplateAnalyzer.BicepCompileErrorMessage)
                    ? ExitCode.ErrorInvalidBicepTemplate
                    : ExitCode.ErrorAnalysis;
            }
        }

        private ExitCode SetupAnalysis(
            FileInfo configurationFile,
            DirectoryInfo directoryToAnalyze,
            ReportFormat reportFormat,
            FileInfo outputFilePath,
            bool includeNonSecurityRules,
            bool verbose)
        {
            // Output file path must be specified if SARIF was chosen as the report format
            if (reportFormat == ReportFormat.Sarif && outputFilePath == null)
            {
                Console.Error.WriteLine("When using --report-format sarif flag, --output-file-path flag is required.");
                return ExitCode.ErrorMissingPath;
            }

            this.reportWriter = GetReportWriter(reportFormat, outputFilePath, directoryToAnalyze?.FullName);
            CreateLoggers(verbose);

            this.templateAnalyzer = TemplateAnalyzer.Create(includeNonSecurityRules, this.logger);

            if (!TryReadConfigurationFile(configurationFile, out var config))
            {
                return ExitCode.ErrorInvalidConfiguration;
            }

            // Success from TryReadConfigurationFile means there wasn't an error looking for the config.
            // config could still be null if no path was specified in the command and no default exists.
            if (config != null)
            {
                this.templateAnalyzer.FilterRules(config);
            }

            return ExitCode.Success;
        }

        private void FinishAnalysis()
        {
            this.summaryLogger.SummarizeLogs();
            this.reportWriter?.Dispose();
        }

        private static IReportWriter GetReportWriter(ReportFormat reportFormat, FileInfo outputFile, string rootFolder = null) =>
            reportFormat switch
            {
                ReportFormat.Sarif => new SarifReportWriter((FileInfoBase)outputFile, rootFolder),
                _ => new ConsoleReportWriter()
            };

        private void CreateLoggers(bool verbose)
        {
            this.summaryLogger = new SummaryLogger(verbose);

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information)
                    .AddConsole(options =>
                    {
                        options.FormatterName = "ConsoleLoggerFormatter";
                    })
                    .AddProvider(new SummaryLoggerProvider(summaryLogger))
                    .AddConsoleFormatter<ConsoleLoggerFormatter, ConsoleLoggerFormatterOptions>(options => options.Verbose = verbose);
            });

            if (this.reportWriter is SarifReportWriter sarifWriter)
            {
                loggerFactory.AddProvider(new SarifNotificationLoggerProvider(sarifWriter.SarifLogger));
            }

            this.logger = loggerFactory.CreateLogger("TemplateAnalyzerCli");
        }

        /// <summary>
        /// Reads a configuration file from disk. If no file was passed, checks the default directory for this file.
        /// </summary>
        /// <returns>True if: 
        ///   - the specified configuration file was read successfully,
        ///   - no config was specified and the default file doesn't exist,
        ///   - the default file exists and was read successfully.
        /// False otherwise.</returns>
        private bool TryReadConfigurationFile(FileInfo configurationFile, out ConfigurationDefinition config)
        {
            config = null;

            string configFilePath;
            if (configurationFile != null)
            {
                if (!configurationFile.Exists)
                {
                    // If a config file was specified in the command but doesn't exist, it's an error.
                    this.logger.LogError("Configuration file does not exist.");
                    return false;
                }
                configFilePath = configurationFile.FullName;
            }
            else
            {
                // Look for a config at the default location.
                // It's not required to exist, so if it doesn't, just return early.
                configFilePath = Path.Combine(AppContext.BaseDirectory, DefaultConfigFileName);
                if (!File.Exists(configFilePath))
                    return true;
            }

            // At this point, an existing config file was found.
            // If there are any problems reading it, it's an error.

            this.logger.LogInformation($"Configuration File: {configFilePath}");

            string configContents;
            try
            {
                configContents = File.ReadAllText(configFilePath);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Unable to read configuration file.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(configContents))
            {
                this.logger.LogError("Configuration is empty.");
                return false;
            }

            try
            {
                config = JsonConvert.DeserializeObject<ConfigurationDefinition>(configContents);
                return true;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Failed to parse configuration file.");
                return false;
            }
        }

        private ExitCode AnalyzeExitCodes(List<ExitCode> exitCodes)
        {
            if (exitCodes.Count == 1)
                return exitCodes[0];

            bool issueReported = exitCodes.Any(x => x == ExitCode.Violation);
            bool filesFailed = exitCodes.Any(x => x == ExitCode.ErrorAnalysis || x == ExitCode.ErrorInvalidBicepTemplate);

            return filesFailed
                ? issueReported ? ExitCode.ErrorAndViolation : ExitCode.ErrorAnalysis
                : issueReported ? ExitCode.Violation : ExitCode.Success;
        }
    }
}