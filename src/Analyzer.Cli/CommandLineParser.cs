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
        public async Task InvokeCommandLineAPIAsync(string[] args)
        {
            await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private RootCommand SetupCommandLineAPI()
        {
            // Command line API is setup using https://github.com/dotnet/command-line-api

            rootCommand = new RootCommand();
            rootCommand.Description = "Analyze Azure Resource Manager (ARM) Templates for security and best practice issues.";

            // Setup analyze-template w/ template file argument and parameter file option
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

            Option<ReportFormat> reportFormatOption = new Option<ReportFormat>(
                "--report-format",
                "Format of report to be generated");
            analyzeTemplateCommand.AddOption(reportFormatOption);

            Option<FileInfo> outputFileOption = new Option<FileInfo>(
                "--output-file-path",
                "The report file path");
            analyzeTemplateCommand.AddOption(outputFileOption);

            analyzeTemplateCommand.Handler = CommandHandler.Create<FileInfo, FileInfo, ReportFormat, FileInfo>((templateFilePath, parametersFilePath, reportFormat, outputFilePath) => this.AnalyzeTemplate(templateFilePath, parametersFilePath, reportFormat, outputFilePath));

            // Setup analyze-directory w/ directory argument 
            Command analyzeDirectoryCommand = new Command(
                "analyze-directory", 
                "Analyze all templates within a directory");

            Argument<DirectoryInfo> directoryArgument = new Argument<DirectoryInfo>(
                "directory-path",
                "The directory to find ARM templates");
            analyzeDirectoryCommand.AddArgument(directoryArgument);

            analyzeDirectoryCommand.AddOption(reportFormatOption);

            analyzeDirectoryCommand.AddOption(outputFileOption);

            analyzeDirectoryCommand.Handler = CommandHandler.Create<DirectoryInfo, ReportFormat, FileInfo>((directoryPath, reportFormat, outputFilePath) => this.AnalyzeDirectory(directoryPath, reportFormat, outputFilePath));

            // Add commands to root command
            rootCommand.AddCommand(analyzeTemplateCommand);
            rootCommand.AddCommand(analyzeDirectoryCommand);

            return rootCommand;
        }

        private int AnalyzeTemplate(FileInfo templateFilePath, FileInfo parametersFilePath, ReportFormat reportFormat, FileInfo outputFilePath, bool printMessageIfNotTemplate = true, IReportWriter writer = null)
        {
            try
            {
                // Check that template file paths exist
                if (!templateFilePath.Exists)
                {
                    Console.WriteLine($"Invalid template file path ({templateFilePath})");
                    return 0;
                }

                // Check that output file path provided for sarif report
                if (writer == null && reportFormat == ReportFormat.Sarif && outputFilePath == null)
                {
                    Console.WriteLine($"Output file path was not provided.");
                    return 0;
                }

                string templateFileContents = File.ReadAllText(templateFilePath.FullName);
                string parameterFileContents = parametersFilePath == null ? null : File.ReadAllText(parametersFilePath.FullName);

                // Check that the schema is valid
                if (!templateFilePath.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase) || !IsValidSchema(templateFileContents))
                {
                    if (printMessageIfNotTemplate)
                    {
                        Console.WriteLine("File is not a valid ARM Template.");
                    }
                    return 0;
                }

                IEnumerable<IEvaluation> evaluations = templateAnalyzer.AnalyzeTemplate(templateFileContents, parameterFileContents, templateFilePath.FullName);

                if (writer == null)
                {
                    using (IReportWriter reportWriter = GetReportWriter(reportFormat.ToString(), outputFilePath))
                    {
                        reportWriter.WriteResults(evaluations, (FileInfoBase)templateFilePath, (FileInfoBase)parametersFilePath);
                    }
                }
                else
                {
                    writer.WriteResults(evaluations, (FileInfoBase)templateFilePath, (FileInfoBase)parametersFilePath);
                }

                return 1;
            }
            catch (Exception exp)
            {
                Console.WriteLine($"An exception occurred: {GetAllExceptionMessages(exp)}");
                return -1;
            }
        }

        private void AnalyzeDirectory(DirectoryInfo directoryPath, ReportFormat reportFormat, FileInfo outputFilePath)
        {
            try
            {
                if (!directoryPath.Exists)
                {
                    Console.WriteLine($"Invalid directory ({directoryPath})");
                    return;
                }

                // Check that output file path provided for sarif report
                if (reportFormat != ReportFormat.Console && outputFilePath == null)
                {
                    Console.WriteLine($"Output file path is not provided.");
                    return;
                }

                // Find files to analyze
                List<FileInfo> filesToAnalyze = new List<FileInfo>();
                FindJsonFilesInDirectoryRecursive(directoryPath, filesToAnalyze);

                // Log root directory
                Console.WriteLine(Environment.NewLine + Environment.NewLine + $"Directory: {directoryPath}");

                int numOfSuccesses = 0;
                using (IReportWriter reportWriter = this.GetReportWriter(reportFormat.ToString(), outputFilePath, directoryPath.FullName))
                {
                    List<FileInfo> filesFailed = new List<FileInfo>();
                    foreach (FileInfo file in filesToAnalyze)
                    {
                        int res = AnalyzeTemplate(file, null, reportFormat, outputFilePath, false, reportWriter);
                        if (res == 1)
                        {
                            numOfSuccesses++;
                        }
                        else if (res == -1)
                        {
                            filesFailed.Add(file);
                        }
                    }

                    Console.WriteLine(Environment.NewLine + $"Analyzed {numOfSuccesses} file(s).");
                    if (filesFailed.Count > 0)
                    {
                        Console.WriteLine($"Unable to analyze {filesFailed.Count} file(s):");
                        foreach (FileInfo failedFile in filesFailed)
                        {
                            Console.WriteLine($"\t{failedFile}");
                        }
                    }
                }

            }
            catch (Exception exp)
            {
                Console.WriteLine($"An exception occurred: {GetAllExceptionMessages(exp)}");
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

        private static string GetAllExceptionMessages(Exception exception)
        {
            string exceptionMessage = exception.Message;

            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
                exceptionMessage += " - " + exception.Message;
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
                    default:
                        return new ConsoleReportWriter();
                }
            }
            return new ConsoleReportWriter();
        }
    }
}