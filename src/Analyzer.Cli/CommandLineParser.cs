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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Creates the command line for running the Template Analyzer. 
    /// Instantiates arguments that can be passed and different commands that can be invoked.
    /// </summary>
    internal class CommandLineParser
    {
        private readonly IReadOnlyList<string> validSchemas = new List<string> {
            "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
            "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
            "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#",
            "https://schema.management.azure.com/schemas/2019-08-01/tenantDeploymentTemplate.json#",
            "https://schema.management.azure.com/schemas/2019-08-01/managementGroupDeploymentTemplate.json#"
        }.AsReadOnly();

        private readonly IReadOnlyList<string> validTemplateProperties = new List<string> {
            "contentVersion",
            "apiProfile",
            "parameters",
            "variables",
            "functions",
            "resources",
            "outputs",
        }.AsReadOnly();

        private const string defaultConfigFileName = "configuration.json";

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
            rootCommand.Description = "Analyze Azure Resource Manager (ARM) Templates for security and best practice issues.";

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
                    "The ARM template to analyze"));

            analyzeTemplateCommand.AddOption(
                new Option<FileInfo>(
                    new[] { "-p", "--parameters-file-path" },
                    "The parameter file to use when parsing the specified ARM template")
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
                    "The directory to find ARM templates"));

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
                    "The configuration file to use when parsing the specified ARM template"),

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
                    "--run-ttk",
                    "Run TTK against templates")
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
            bool runTtk,
            bool verbose)
        {
            // Check that template file paths exist
            if (!templateFilePath.Exists)
            {
                Console.Error.WriteLine("Invalid template file path: {0}", templateFilePath);
                return (int)ExitCode.ErrorInvalidPath;
            }

            var setupResult = SetupAnalysis(configFilePath, directoryToAnalyze: null, reportFormat, outputFilePath, runTtk, verbose);
            if (setupResult != ExitCode.Success)
            {
                return (int)setupResult;
            }

            // Verify the file is a valid template
            if (!IsValidTemplate(templateFilePath))
            {
                logger.LogError("File is not a valid ARM Template. File path: {templateFilePath}", templateFilePath.FullName);
                FinishAnalysis();
                return (int)ExitCode.ErrorInvalidARMTemplate;
            }

            var analysisResult = AnalyzeTemplate(templateFilePath, parametersFilePath);

            FinishAnalysis();
            return (int)analysisResult;
        }

        // Note: argument names must match command arguments/options (without "-" characters)
        private int AnalyzeDirectoryCommandHandler(
            DirectoryInfo directoryPath,
            FileInfo configFilePath,
            ReportFormat reportFormat,
            FileInfo outputFilePath,
            bool runTtk,
            bool verbose)
        {
            if (!directoryPath.Exists)
            {
                Console.Error.WriteLine("Invalid directory: {0}", directoryPath);
                return (int)ExitCode.ErrorInvalidPath;
            }

            var setupResult = SetupAnalysis(configFilePath, directoryPath, reportFormat, outputFilePath, runTtk, verbose);
            if (setupResult != ExitCode.Success)
            {
                return (int)setupResult;
            }

            // Find files to analyze
            var filesToAnalyze = FindTemplateFilesInDirectory(directoryPath);

            // Log root directory info to be analyzed
            Console.WriteLine(Environment.NewLine + Environment.NewLine + $"Directory: {directoryPath}");

            int numOfFilesAnalyzed = 0;
            bool issueReported = false;
            var filesFailed = new List<FileInfo>();
            foreach (FileInfo file in filesToAnalyze)
            {
                ExitCode res = AnalyzeTemplate(file, null);

                if (res == ExitCode.Success || res == ExitCode.Violation)
                {
                    numOfFilesAnalyzed++;
                    issueReported |= res == ExitCode.Violation;
                }
                else if (res == ExitCode.ErrorAnalysis)
                {
                    filesFailed.Add(file);
                }
            }

            Console.WriteLine(Environment.NewLine + $"Analyzed {numOfFilesAnalyzed} {(numOfFilesAnalyzed == 1 ? "file" : "files")}.");

            ExitCode exitCode;
            if (filesFailed.Count > 0)
            {
                logger.LogError($"Unable to analyze {filesFailed.Count} {(filesFailed.Count == 1 ? "file" : "files")}: {string.Join(", ", filesFailed)}");
                exitCode = issueReported ? ExitCode.ErrorAndViolation : ExitCode.ErrorAnalysis;
            }
            else
            {
                exitCode = issueReported ? ExitCode.Violation : ExitCode.Success;
            }
            
            FinishAnalysis();
            
            return (int)exitCode;
        }

        private ExitCode AnalyzeTemplate(FileInfo templateFilePath, FileInfo parametersFilePath)
        {
            try
            {
                string templateFileContents = File.ReadAllText(templateFilePath.FullName);
                string parameterFileContents = parametersFilePath == null ? null : File.ReadAllText(parametersFilePath.FullName);

                IEnumerable<IEvaluation> evaluations = this.templateAnalyzer.AnalyzeTemplate(templateFileContents, parameterFileContents, templateFilePath.FullName);

                this.reportWriter.WriteResults(evaluations, (FileInfoBase)templateFilePath, (FileInfoBase)parametersFilePath);

                return evaluations.Any(e => !e.Passed) ? ExitCode.Violation : ExitCode.Success;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An exception occurred while analyzing a template");
                return ExitCode.ErrorAnalysis;
            }
        }

        private ExitCode SetupAnalysis(
            FileInfo configurationFile,
            DirectoryInfo directoryToAnalyze,
            ReportFormat reportFormat,
            FileInfo outputFilePath,
            bool runPowershell,
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

            this.templateAnalyzer = TemplateAnalyzer.Create(runPowershell, this.logger);

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

        private IEnumerable<FileInfo> FindTemplateFilesInDirectory(DirectoryInfo directoryPath)
        {
            var armTemplates = directoryPath.GetFiles(
                "*.json",
                new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = true
                }
            ).Where(IsValidTemplate);

            var bicepTemplates = directoryPath.GetFiles(
                "*.bicep",
                new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = true
                });

            return armTemplates.Concat(bicepTemplates);
        }

        private bool IsValidTemplate(FileInfo file)
        {
            // bicep files are valid
            if (file.FullName.EndsWith(".bicep", StringComparison.OrdinalIgnoreCase) )
            {
                return true;
            }

            using var fileStream = new StreamReader(file.OpenRead());
            var reader = new JsonTextReader(fileStream);

            reader.Read();
            if (reader.TokenType != JsonToken.StartObject)
            {
                return false;
            }

            while (reader.Read())
            {
                if (reader.Depth == 1 && reader.TokenType == JsonToken.PropertyName)
                {
                    if (string.Equals((string)reader.Value, "$schema", StringComparison.OrdinalIgnoreCase))
                    {
                        reader.Read();
                        if (reader.TokenType != JsonToken.String)
                        {
                            return false;
                        }

                        return validSchemas.Any(schema => string.Equals((string)reader.Value, schema, StringComparison.OrdinalIgnoreCase));
                    }
                    else if (!validTemplateProperties.Any(property => string.Equals((string)reader.Value, property, StringComparison.OrdinalIgnoreCase)))
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private static IReportWriter GetReportWriter(ReportFormat reportFormat, FileInfo outputFile, string rootFolder = null) =>
            reportFormat switch {
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
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                    })
                    .AddProvider(new SummaryLoggerProvider(summaryLogger));
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
                configFilePath = Path.Combine(AppContext.BaseDirectory, defaultConfigFileName);
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
    }
}