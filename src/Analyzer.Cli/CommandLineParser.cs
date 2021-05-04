// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    internal class CommandLineParser
    {
        RootCommand rootCommand;

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
        public async Task InvokeCommandLineAPIAsync(string[] args)
        {
            await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private RootCommand SetupCommandLineAPI()
        {
            // Command line API is setup using https://github.com/dotnet/command-line-api
            // Create a root command 
            rootCommand = new RootCommand();
            rootCommand.Description = "Analyze Azure Resource Manager (ARM) Templates for security and best practice issues.";

            // It has two commands - analyze-template and analyze-directory
            rootCommand.AddCommand(SetupAnalyzeTemplateCommand());

            rootCommand.AddCommand(SetupAnalyzeDirectoryCommand());
            
            return rootCommand;
        }

        private Command SetupAnalyzeTemplateCommand()
        {
            // Setup analyze-template 
            Command analyzeTemplateCommand = new Command("analyze-template");

            Option<FileInfo> templateOption = new Option<FileInfo>(
                    "--template-file-path",
                    "The ARM template to analyze")
            {
                IsRequired = true
            };
            templateOption.AddAlias("-t");

            analyzeTemplateCommand.AddOption(templateOption);

            Option<FileInfo> parameterOption = new Option<FileInfo>(
                 "--parameters-file-path",
                 "The parameter file to use when parsing the specified ARM template");
            parameterOption.AddAlias("-p");

            analyzeTemplateCommand.AddOption(parameterOption);

            analyzeTemplateCommand.AddOption(SetupOutputFileOption());

            analyzeTemplateCommand.Handler = CommandHandler.Create<FileInfo, FileInfo>((templateFilePath, parametersFilePath) =>
            {
                try
                {
                    Core.TemplateAnalyzer templateAnalyzer = new Core.TemplateAnalyzer(File.ReadAllText(templateFilePath.FullName), parametersFilePath == null ? null : File.ReadAllText(parametersFilePath.FullName));
                    IEnumerable<Types.IEvaluation> evaluations = templateAnalyzer.EvaluateRulesAgainstTemplate();

                    foreach (var evaluation in evaluations)
                    {
                        string resultString = GenerateResultString(evaluation);
                        string fileMetadata = $"\n\tFile: {templateFilePath}";
                        if (parametersFilePath != null)
                        {
                            fileMetadata += $"\n\tParameters File: {parametersFilePath}";
                        };
                        Console.WriteLine($"\n\n{evaluation.RuleName}: {evaluation.RuleDescription}{fileMetadata}\n\tResult: {evaluation.Passed} {resultString}");
                    }
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"An exception occured: {exp.Message}");
                }

            });

            return analyzeTemplateCommand;
        }

        private string GenerateResultString(Types.IEvaluation evaluation)
        {
            string resultString = "";

            if (!evaluation.Passed)
            {
                if (evaluation.Results.Count() > 0)
                {
                    foreach (var result in evaluation.Results)
                    {
                        resultString += $"\n\tLine: {result.LineNumber}";
                    }
                }
                else
                {
                    foreach (var innerEvaluation in evaluation.Evaluations)
                    {
                        resultString += GenerateResultString(innerEvaluation);
                    }
                }
            }

            return resultString;
        }

        private Command SetupAnalyzeDirectoryCommand()
        {
            // Setup analyze-directory 
            Command analyzeDirectoryCommand = new Command("analyze-directory");

            Option<DirectoryInfo> directoryOption = new Option<DirectoryInfo>(
                    "--directory-path",
                    "Directory to search for ARM templates (.json file extension)")
            {
                IsRequired = true
            };
            directoryOption.AddAlias("-d");

            analyzeDirectoryCommand.AddOption(directoryOption);

            Option<bool> recursiveOption = new Option<bool>(
                    "--recursive",
                    "Search directory and all subdirectories");
            recursiveOption.AddAlias("-r");

            analyzeDirectoryCommand.AddOption(recursiveOption);

            analyzeDirectoryCommand.AddOption(SetupOutputFileOption());

            analyzeDirectoryCommand.Handler = CommandHandler.Create<DirectoryInfo, bool>((directoryPath, recursive) =>
            {
                // TODO: This needs to call the library and pass in a list of templates
            });

            return analyzeDirectoryCommand;
        }

        private Option SetupOutputFileOption()
        {
            Option<FileInfo> outputFileOption = new Option<FileInfo>(
                        "--output-path",
                        "Redirect output to specified file in JSON format");

            outputFileOption.AddAlias("-o");

            return outputFileOption;
        }
    }
}
