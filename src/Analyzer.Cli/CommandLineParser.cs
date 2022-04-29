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
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Creates the command line for running the Template Analyzer. 
    /// Instantiates arguments that can be passed and different commands that can be invoked.
    /// </summary>
    internal class CommandLineParser
    {
        RootCommand rootCommand;

        private readonly TemplateAnalyzer templateAnalyzer;

        /// <summary>
        /// Constructor for the command line parser. Sets up the command line API. 
        /// </summary>
        public CommandLineParser()
        {
            SetupCommandLineAPI();
            templateAnalyzer = TemplateAnalyzer.Create();
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
                this.AnalyzeTemplate(templateFilePath, parametersFilePath, configFilePath, reportFormat, outputFilePath, runTtk, verbose));

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
                this.AnalyzeDirectory(directoryPath, configurationsFilePath,  reportFormat, outputFilePath, runTtk, verbose));

            // Add commands to root command
            rootCommand.AddCommand(analyzeTemplateCommand);
            rootCommand.AddCommand(analyzeDirectoryCommand);

            return rootCommand;
        }

        private int AnalyzeTemplate(FileInfo templateFilePath, FileInfo parametersFilePath, FileInfo configurationsFilePath, ReportFormat reportFormat, FileInfo outputFilePath, bool runTtk, bool verbose, bool printMessageIfNotTemplate = true, IReportWriter writer = null, bool readConfigurationFile = true, ILogger logger = null)
        { 
            bool disposeWriter = false;

            if (writer == null)
            {
                if (reportFormat == ReportFormat.Sarif && outputFilePath == null)
                {
                    // We can't use the logger for this error,
                    // because we need to get the writer to create the logger,
                    // but this check has to be done before getting the writer:
                    Console.WriteLine("When using --report-format sarif flag, --output-file-path flag is required.");
                    return (int)ExitCode.ErrorMissingPath;
                }

                writer = GetReportWriter(reportFormat, outputFilePath);
                disposeWriter = true;
            }

            if (logger == null)
            {
                logger = CreateLogger(verbose, reportFormat, writer);
            }

            try
            {
                // Check that template file paths exist
                if (!templateFilePath.Exists)
                {
                    logger.LogError("Invalid template file path: {templateFilePath}", templateFilePath);
                    return (int)ExitCode.ErrorInvalidPath;
                }

                string templateFileContents = File.ReadAllText(templateFilePath.FullName);
                string parameterFileContents = parametersFilePath == null ? null : File.ReadAllText(parametersFilePath.FullName);

                if (readConfigurationFile)
                {
                    templateAnalyzer.FilterRules(configurationsFilePath);
                }

                // Check that the schema is valid
                if (!templateFilePath.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase) || !IsValidSchema(templateFileContents))
                {
                    if (printMessageIfNotTemplate)
                    {
                        logger.LogError("File is not a valid ARM Template. File path: {templateFilePath}", templateFilePath);
                    }
                    return (int)ExitCode.ErrorInvalidARMTemplate;
                }

                IEnumerable<IEvaluation> evaluations = templateAnalyzer.AnalyzeTemplate(templateFileContents, parameterFileContents, templateFilePath.FullName, usePowerShell: runTtk, logger);

                writer.WriteResults(evaluations, (FileInfoBase)templateFilePath, (FileInfoBase)parametersFilePath);

                return evaluations.Any(e => !e.Passed) ? (int)ExitCode.Violation : (int)ExitCode.Success;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An exception occurred while analyzing a template");
                return (int)ExitCode.ErrorGeneric;
            }
            finally
            {
                if (disposeWriter && writer != null)
                {
                    writer.Dispose();
                }
            }
        }

        private int AnalyzeDirectory(DirectoryInfo directoryPath, FileInfo configurationsFilePath, ReportFormat reportFormat, FileInfo outputFilePath, bool runTtk, bool verbose)
        {
            // Check that output file path provided for sarif report
            if (reportFormat == ReportFormat.Sarif && outputFilePath == null)
            {
                // We can't use the logger for this error,
                // because we need to get the writer to create the logger,
                // but this check has to be done before getting the writer:
                Console.WriteLine("When using --report-format sarif flag, --output-file-path flag is required.");
                return (int)ExitCode.ErrorMissingPath;
            }

            using var reportWriter = GetReportWriter(reportFormat, outputFilePath, directoryPath.FullName);

            var logger = CreateLogger(verbose, reportFormat, reportWriter);

            try
            {
                if (!directoryPath.Exists)
                {
                    logger.LogError("Invalid directory: {directoryPath}", directoryPath);
                    return (int)ExitCode.ErrorInvalidPath;
                }

                templateAnalyzer.FilterRules(configurationsFilePath);

                // Find files to analyze
                var filesToAnalyze = new List<FileInfo>();
                FindJsonFilesInDirectoryRecursive(directoryPath, filesToAnalyze);

                // Log root directory info to be analyzed
                Console.WriteLine(Environment.NewLine + Environment.NewLine + $"Directory: {directoryPath}");

                int numOfFilesAnalyzed = 0;
                bool issueReported = false;
                var filesFailed = new List<FileInfo>();
                foreach (FileInfo file in filesToAnalyze)
                {
                    int res = AnalyzeTemplate(file, null, configurationsFilePath, reportFormat, outputFilePath, runTtk, verbose, false, reportWriter, false, logger);
                    if (res == (int)ExitCode.Success)
                    {
                        numOfFilesAnalyzed++;
                    }
                    else if (res == (int)ExitCode.Violation)
                    {
                        numOfFilesAnalyzed++;
                        issueReported = true;
                    }
                    else if (res == (int)ExitCode.ErrorGeneric)
                    {
                        filesFailed.Add(file);
                    }
                }

                Console.WriteLine(Environment.NewLine + $"Analyzed {numOfFilesAnalyzed} file(s).");
                if (filesFailed.Count > 0)
                {
                    logger.LogError($"Unable to analyze {filesFailed.Count} file(s): {string.Join(", ", filesFailed)}");
                    return (int)(issueReported ? ExitCode.ErrorAndViolation : ExitCode.ErrorGeneric);
                }
                return issueReported ? (int)ExitCode.Violation : (int)ExitCode.Success;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An exception occurred while analyzing the directory provided");
                return (int)ExitCode.ErrorGeneric;
            }
        }

        private void FindJsonFilesInDirectoryRecursive(DirectoryInfo directoryPath, List<FileInfo> files) 
        {
            foreach (FileInfo file in directoryPath.GetFiles())
            {
                if (file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(file);
                }
            }
            foreach (DirectoryInfo dir in directoryPath.GetDirectories())
            {
                FindJsonFilesInDirectoryRecursive(dir, files);
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

        private static IReportWriter GetReportWriter(ReportFormat reportFormat, FileInfo outputFile, string rootFolder = null)
        {
            switch (reportFormat)
            {
                case ReportFormat.Sarif:
                    return new SarifReportWriter((FileInfoBase)outputFile, rootFolder);
                case ReportFormat.Console:
                    return new ConsoleReportWriter();
                default:
                    return new ConsoleReportWriter();
            }
        }

        private static ILogger CreateLogger(bool verbose, ReportFormat reportFormat, IReportWriter reportWriter)
        {
            var logLevel = verbose ? LogLevel.Debug : LogLevel.Information;

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(logLevel)
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                    });
            });

            if (reportFormat == ReportFormat.Sarif)
            {
                var sarifLogger = ((SarifReportWriter)reportWriter).SarifLogger;

                loggerFactory.AddProvider(new SarifNotificationLoggerProvider(sarifLogger));
            }

            return loggerFactory.CreateLogger("TemplateAnalyzerCLI");
        }
    }
}