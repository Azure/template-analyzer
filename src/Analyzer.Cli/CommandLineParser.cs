// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Templates.Analyzer.Core;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Reports;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Creates the command line for running the Template Analyzer. 
    /// Instantiates arguments that can be passed and different commands that can be invoked.
    /// </summary>
    internal class CommandLineParser
    {
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

        private RootCommand SetupCommandLineAPI()
        {
            // Command line API is setup using https://github.com/dotnet/command-line-api

            rootCommand = new RootCommand();
            rootCommand.Description = "Analyze Azure Resource Manager (ARM) Templates for security and best practice issues.";

            // Setup analyze-template w/ template file argument, parameter file option, and configuration file option
            Command analyzeTemplateCommand = new Command(
                "analyze-template",
                "Analyze a singe template");
            
            Argument<FileInfo> templateArgument = new Argument<FileInfo>(
                "template-file-path",
                "The ARM template to analyze");
            analyzeTemplateCommand.AddArgument(templateArgument);

            Option<FileInfo> parameterOption = new Option<FileInfo>(
                 "--parameters-file-path",
                 "The parameter file to use when parsing the specified ARM template");
            parameterOption.AddAlias("-p");
            analyzeTemplateCommand.AddOption(parameterOption);

            Option<FileInfo> configurationOption = new Option<FileInfo>(
                 "--config-file-path",
                 "The configuration file to use when parsing the specified ARM template");
            configurationOption.AddAlias("-c");
            analyzeTemplateCommand.AddOption(configurationOption);

            Option<ReportFormat> reportFormatOption = new Option<ReportFormat>(
                "--report-format",
                "Format of report to be generated");
            analyzeTemplateCommand.AddOption(reportFormatOption);

            Option<FileInfo> outputFileOption = new Option<FileInfo>(
                "--output-file-path",
                "The report file path");
            outputFileOption.AddAlias("-o");
            analyzeTemplateCommand.AddOption(outputFileOption);

            var verboseOption = new Option(
                "--verbose",
                "Shows details about the analysis");
            verboseOption.AddAlias("-v");
            analyzeTemplateCommand.AddOption(verboseOption);

            // Temporary PowerShell rule suppression until it will work nicely with SARIF
            Option ttkOption = new Option(
                "--run-ttk",
                "Run TTK against templates");
            analyzeTemplateCommand.AddOption(ttkOption);

            analyzeTemplateCommand.Handler = CommandHandler.Create<FileInfo, FileInfo, FileInfo, ReportFormat, FileInfo, bool, bool>(
                (templateFilePath, parametersFilePath, configFilePath, reportFormat, outputFilePath, runTtk, verbose) =>
                this.AnalyzeTemplateCommandHandler(templateFilePath, parametersFilePath, configFilePath, reportFormat, outputFilePath, runTtk, verbose));

            // Setup analyze-directory w/ directory argument and configuration file option
            Command analyzeDirectoryCommand = new Command(
                "analyze-directory", 
                "Analyze all templates within a directory");

            Argument<DirectoryInfo> directoryArgument = new Argument<DirectoryInfo>(
                "directory-path",
                "The directory to find ARM templates");
            analyzeDirectoryCommand.AddArgument(directoryArgument);

            analyzeDirectoryCommand.AddOption(configurationOption);
          
            analyzeDirectoryCommand.AddOption(reportFormatOption);
          
            analyzeDirectoryCommand.AddOption(outputFileOption);
          
            analyzeDirectoryCommand.AddOption(ttkOption);

            analyzeDirectoryCommand.AddOption(verboseOption);

            analyzeDirectoryCommand.Handler = CommandHandler.Create<DirectoryInfo, FileInfo, ReportFormat, FileInfo, bool, bool>(
                (directoryPath, configurationsFilePath, reportFormat, outputFilePath, runTtk, verbose) =>
                this.AnalyzeDirectoryCommandHandler(directoryPath, configurationsFilePath,  reportFormat, outputFilePath, runTtk, verbose));

            // Add commands to root command
            rootCommand.AddCommand(analyzeTemplateCommand);
            rootCommand.AddCommand(analyzeDirectoryCommand);

            return rootCommand;
        }

        private int AnalyzeTemplateCommandHandler(
            FileInfo templateFilePath,
            FileInfo parametersFilePath,
            FileInfo configurationFilePath,
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

            var setupResult = SetupAnalysis(configurationFilePath, directoryToAnalyze: null, reportFormat, outputFilePath, runTtk, verbose);
            if (setupResult != ExitCode.Success)
            {
                return (int)setupResult;
            }

            // Check that the schema is valid
            if (!templateFilePath.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase) || !IsValidSchema(File.ReadAllText(templateFilePath.FullName)))
            {
                logger.LogError("File is not a valid ARM Template. File path: {templateFilePath}", templateFilePath);
                FinishAnalysis();
                return (int)ExitCode.ErrorInvalidARMTemplate;
            }

            var analysisResult = AnalyzeTemplate(templateFilePath, parametersFilePath, this.reportWriter, this.logger);

            FinishAnalysis();
            return (int)analysisResult;
        }

        private int AnalyzeDirectoryCommandHandler(
            DirectoryInfo directoryPath,
            FileInfo configurationFilePath,
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

            var setupResult = SetupAnalysis(configurationFilePath, directoryPath, reportFormat, outputFilePath, runTtk, verbose);
            if (setupResult != ExitCode.Success)
            {
                return (int)setupResult;
            }

            // Find files to analyze
            var filesToAnalyze = new List<FileInfo>();
            FindTemplateFilesInDirectoryRecursive(directoryPath, filesToAnalyze);

            // Log root directory info to be analyzed
            Console.WriteLine(Environment.NewLine + Environment.NewLine + $"Directory: {directoryPath}");

            int numOfFilesAnalyzed = 0;
            bool issueReported = false;
            var filesFailed = new List<FileInfo>();
            foreach (FileInfo file in filesToAnalyze)
            {
                ExitCode res = AnalyzeTemplate(file, null, reportWriter, logger);

                if (res == ExitCode.Success || res == ExitCode.Violation)
                {
                    numOfFilesAnalyzed++;
                    issueReported |= res == ExitCode.Violation;
                }
                else if (res == ExitCode.ErrorGeneric)
                {
                    filesFailed.Add(file);
                }
            }

            Console.WriteLine(Environment.NewLine + $"Analyzed {numOfFilesAnalyzed} {(numOfFilesAnalyzed == 1 ? "file" : "files")}.");

            ExitCode exitCode;
            if (filesFailed.Count > 0)
            {
                logger.LogError($"Unable to analyze {filesFailed.Count} {(filesFailed.Count == 1 ? "file" : "files")}: {string.Join(", ", filesFailed)}");
                exitCode = issueReported ? ExitCode.ErrorAndViolation : ExitCode.ErrorGeneric;
            }
            else
            {
                exitCode = issueReported ? ExitCode.Violation : ExitCode.Success;
            }
            
            this.summaryLogger.SummarizeLogs();
            FinishAnalysis();
            
            return (int)exitCode;
        }

        private ExitCode AnalyzeTemplate(FileInfo templateFilePath, FileInfo parametersFilePath, IReportWriter writer, ILogger logger)
        { 
            try
            {
                string templateFileContents = File.ReadAllText(templateFilePath.FullName);
                string parameterFileContents = parametersFilePath == null ? null : File.ReadAllText(parametersFilePath.FullName);

                IEnumerable<IEvaluation> evaluations = this.templateAnalyzer.AnalyzeTemplate(templateFileContents, parameterFileContents, templateFilePath.FullName, logger);

                writer.WriteResults(evaluations, (FileInfoBase)templateFilePath, (FileInfoBase)parametersFilePath);

                return evaluations.Any(e => !e.Passed) ? ExitCode.Violation : ExitCode.Success;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An exception occurred while analyzing a template");
                return ExitCode.ErrorGeneric;
            }
        }

        private ExitCode SetupAnalysis(
            FileInfo configurationFilePath,
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

            this.templateAnalyzer = TemplateAnalyzer.Create(runPowershell);

            if (configurationFilePath != null)
            {
                ConfigurationDefinition config = ReadConfigurationFile(configurationFilePath);
                if (config == null)
                    return ExitCode.ErrorGeneric;

                this.templateAnalyzer.FilterRules(config);
            }

            return ExitCode.Success;
        }

        private void FinishAnalysis()
        {
            this.reportWriter?.Dispose();
        }

        private void FindTemplateFilesInDirectoryRecursive(DirectoryInfo directoryPath, List<FileInfo> files) 
        {
            foreach (FileInfo file in directoryPath.GetFiles())
            {
                if (file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase) && IsValidSchema(File.ReadAllText(file.FullName)))
                {
                    files.Add(file);
                }
            }
            foreach (DirectoryInfo dir in directoryPath.GetDirectories())
            {
                FindTemplateFilesInDirectoryRecursive(dir, files);
            }
        }

        private bool IsValidSchema(string template)
        {
            JObject jsonTemplate = JObject.Parse(template);
            string schema = (string)jsonTemplate["$schema"];
            string[] validSchemas = { 
                "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
                "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
                "https://schema.management.azure.com/schemas/2018-05-01/subscriptionDeploymentTemplate.json#",
                "https://schema.management.azure.com/schemas/2019-08-01/tenantDeploymentTemplate.json#",
                "https://schema.management.azure.com/schemas/2019-08-01/managementGroupDeploymentTemplate.json#"};
            return validSchemas.Contains(schema);
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

            this.logger = loggerFactory.CreateLogger("TemplateAnalyzerCLI");
        }

        /// <summary>
        /// Reads a configuration file from disk. If no file was passed, checks the default directory for this file.
        /// </summary>
        private ConfigurationDefinition ReadConfigurationFile(FileInfo configurationFilePath)
        {
            this.logger.LogInformation($"Configuration File: {configurationFilePath.FullName}");

            if (!configurationFilePath.Exists)
            {
                this.logger.LogError("Configuration file does not exist.");
                return null;
            }
            
            string configContents;
            try
            {
                configContents = File.ReadAllText(configurationFilePath.FullName);
            }
            catch (Exception e)
            {
                this.logger.LogError("Unable to read configuration file.", e);
                return null;
            }

            if (string.IsNullOrWhiteSpace(configContents))
            {
                this.logger.LogError("Configuration is empty.");
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<ConfigurationDefinition>(configContents);
            }
            catch (Exception e)
            {
                this.logger.LogError("Failed to parse configuration file.", e);
                return null;
            }
        }
    }
}