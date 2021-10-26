// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Templates.Analyzer.Core;
using Microsoft.Azure.Templates.Analyzer.Types;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    internal class CommandLineParser
    {
        RootCommand rootCommand;
        private readonly string IndentedNewLine = Environment.NewLine + "\t";
        private readonly string TwiceIndentedNewLine = Environment.NewLine + "\t\t";

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
            
            analyzeTemplateCommand.Handler = CommandHandler.Create<FileInfo, FileInfo>((templateFilePath, parametersFilePath) => this.AnalyzeTemplate(templateFilePath, parametersFilePath));

            // Setup analyze-directory w/ directory argument 
            Command analyzeDirectoryCommand = new Command(
                "analyze-directory", 
                "Analyze all templates within a directory");

            Argument<DirectoryInfo> directoryArgument = new Argument<DirectoryInfo>(
                "directory-path",
                "The directory to find ARM templates");
            analyzeDirectoryCommand.AddArgument(directoryArgument);

            analyzeDirectoryCommand.Handler = CommandHandler.Create<DirectoryInfo>((directoryPath) => this.AnalyzeDirectory(directoryPath));

            // Add commands to root command
            rootCommand.AddCommand(analyzeTemplateCommand);
            rootCommand.AddCommand(analyzeDirectoryCommand);

            return rootCommand;
        }

        private int AnalyzeTemplate(FileInfo templateFilePath, FileInfo parametersFilePath, bool printMessageIfNotTemplate = true)
        {
            try
            {
                // Check that template file paths exist
                if (!templateFilePath.Exists)
                {
                    Console.WriteLine($"Invalid template file path ({templateFilePath})");
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

                // Log info on file to be analyzed
                string fileMetadata = Environment.NewLine + Environment.NewLine + $"File: {templateFilePath}";
                if (parametersFilePath != null)
                {
                    fileMetadata += Environment.NewLine + $"Parameters File: {parametersFilePath}";
                }
                Console.WriteLine(fileMetadata);

                IEnumerable<IEvaluation> evaluations = templateAnalyzer.AnalyzeTemplate(templateFileContents, parameterFileContents, templateFilePath.FullName);

                var passedEvaluations = 0;

                foreach (var evaluation in evaluations)
                {
                    string resultString = GenerateResultString(evaluation);

                    if (!evaluation.Passed)
                    {
                        var output = $"{IndentedNewLine}{(evaluation.RuleId != "" ? $"{evaluation.RuleId}: " : "")}{evaluation.RuleDescription}" +
                        $"{TwiceIndentedNewLine}More information: {evaluation.HelpUri}" +
                        $"{TwiceIndentedNewLine}Result: {(evaluation.Passed ? "Passed" : "Failed")} {resultString}";
                        Console.WriteLine(output);
                    }
                    else
                    {
                        passedEvaluations++;
                    }
                }

                Console.WriteLine($"{IndentedNewLine}Rules passed: {passedEvaluations}");

                return 1;
            }
            catch (Exception exp)
            {
                Console.WriteLine($"An exception occurred: {GetAllExceptionMessages(exp)}");
                return -1;
            }
        }

        private void AnalyzeDirectory(DirectoryInfo directoryPath)
        {
            try
            {
                if (!directoryPath.Exists)
                {
                    Console.WriteLine($"Invalid directory ({directoryPath})");
                    return;
                }

                // Find files to analyze
                List<FileInfo> filesToAnalyze = new List<FileInfo>();
                FindJsonFilesInDirectoryRecursive(directoryPath, filesToAnalyze);

                // Log root directory
                Console.WriteLine(Environment.NewLine + Environment.NewLine + $"Directory: {directoryPath}");

                int numOfSuccesses = 0;
                List<FileInfo> filesFailed = new List<FileInfo>();
                foreach (FileInfo file in filesToAnalyze)
                {
                    int res = AnalyzeTemplate(file, null, false);
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

        private string GenerateResultString(Types.IEvaluation evaluation)
        {
            string resultString = "";

            if (!evaluation.Passed)
            {
                foreach (var result in evaluation.Results)
                {
                    if (!result.Passed)
                    {
                        resultString += $"{TwiceIndentedNewLine}Line: {result.LineNumber}";
                    }
                }

                foreach (var innerEvaluation in evaluation.Evaluations)
                {
                    resultString += GenerateResultString(innerEvaluation);
                }
            }

            return resultString;
        }
    }
}