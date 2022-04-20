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
                (templateFilePath, parametersFilePath, configurationsFilePath, reportFormat, outputFilePath, runTtk, verbose) =>
                this.AnalyzeTemplate(templateFilePath, parametersFilePath, configurationsFilePath, reportFormat, outputFilePath, runTtk, verbose));

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
            if (logger == null) {
                logger = CreateLogger(verbose);
            }

            bool disposeWriter = false;

            try
            {
                // Check that template file paths exist
                if (!templateFilePath.Exists)
                {
                    logger.LogError("Invalid template file path: {templateFilePath}", templateFilePath);
                    return 2;
                }

                // Check that output file path provided for sarif report
                if (writer == null && reportFormat == ReportFormat.Sarif && outputFilePath == null)
                {
                    logger.LogError("Output file path was not provided.");
                    return 3;
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
                    return 4;
                }

                IEnumerable<IEvaluation> evaluations = templateAnalyzer.AnalyzeTemplate(templateFileContents, parameterFileContents, templateFilePath.FullName, usePowerShell: runTtk, logger);

                if (writer == null)
                {
                    writer = GetReportWriter(reportFormat.ToString(), outputFilePath);
                    disposeWriter = true;
                }

                writer.WriteResults(evaluations, (FileInfoBase)templateFilePath, (FileInfoBase)parametersFilePath);

                return 0;
            }
            catch (Exception exp)
            {
                logger.LogError(GetExceptionMessage(exp));
                return 1;
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
            var logger = CreateLogger(verbose);

            try
            {
                if (!directoryPath.Exists)
                {
                    logger.LogError("Invalid directory: {directoryPath}", directoryPath);
                    return 2;
                }

                // Check that output file path provided for sarif report
                if (reportFormat == ReportFormat.Sarif && outputFilePath == null)
                {
                    logger.LogError("Output file path was not provided.");
                    return 3;
                }

                templateAnalyzer.FilterRules(configurationsFilePath);

                // Find files to analyze
                var filesToAnalyze = new List<FileInfo>();
                FindJsonFilesInDirectoryRecursive(directoryPath, filesToAnalyze);

                // Log root directory info to be analyzed
                Console.WriteLine(Environment.NewLine + Environment.NewLine + $"Directory: {directoryPath}");

                int numOfSuccesses = 0;
                int exitCode = 0;
                using (IReportWriter reportWriter = this.GetReportWriter(reportFormat.ToString(), outputFilePath, directoryPath.FullName))
                {
                    var filesFailed = new List<FileInfo>();
                    foreach (FileInfo file in filesToAnalyze)
                    {
                        int res = AnalyzeTemplate(file, null, configurationsFilePath, reportFormat, outputFilePath, runTtk, verbose, false, reportWriter, false, logger);
                        if (res == 0)
                        {
                            numOfSuccesses++;
                        }
                        else if (res == 1)
                        {
                            filesFailed.Add(file);
                            exitCode = CalculateExitCode(exitCode, res);
                        }
                        else
                        {
                            exitCode = CalculateExitCode(exitCode, res);
                        }
                    }

                    Console.WriteLine(Environment.NewLine + $"Analyzed {numOfSuccesses} file(s).");
                    if (filesFailed.Count > 0)
                    {
                        logger.LogError("Unable to analyze {numFilesFailed} file(s):", filesFailed.Count);
                        foreach (FileInfo failedFile in filesFailed)
                        {
                            logger.LogError("\t{failedFile}", failedFile);
                        }
                        return 6;
                    }
                    return exitCode;
                }
            }
            catch (Exception exp)
            {
                logger.LogError(GetExceptionMessage(exp));
                return 1;
            }
        }

        private int CalculateExitCode(int exitCode, int res)
        {
            if (exitCode == 6 || res == 6)
                return 6;
            else if ((exitCode == 5 && res >= 1 && res <= 4) || (res == 5 && exitCode >= 1 && exitCode <= 4))
                return 6;
            else if (res == 5)
                return 5;
            else
                return Math.Max(exitCode, res);
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

        private static string GetExceptionMessage(Exception exception)
        {
            Func<Exception, string> getExceptionInfo = (exception) => "\n\n" + exception.Message + "\n" + exception.StackTrace;
            
            string exceptionMessage = "An exception occurred:" + getExceptionInfo(exception);

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                exceptionMessage += getExceptionInfo(exception);
            }

            return exceptionMessage;
        }

        private IReportWriter GetReportWriter(string reportFormat, FileInfo outputFile, string rootFolder = null)
        {
            if (Enum.TryParse<ReportFormat>(reportFormat, ignoreCase:true, out ReportFormat format))
            {
                switch (format)
                {
                    case ReportFormat.Sarif:
                        return new SarifReportWriter((FileInfoBase)outputFile, rootFolder);
                    case ReportFormat.Console:
                        return new ConsoleReportWriter();
                }
            }
            return new ConsoleReportWriter();
        }

        private static ILogger CreateLogger(bool verbose)
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

            return loggerFactory.CreateLogger("TemplateAnalyzerCLI");
        }
    }
}
