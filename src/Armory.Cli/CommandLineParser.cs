// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Armory.Cli
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
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task InvokeCommandLineAPIAsync(string[] args)
        {
            await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }

        private RootCommand SetupCommandLineAPI()
        {
            // Command line API is setup using https://github.com/dotnet/command-line-api
            // Create a root command 
            rootCommand = new RootCommand();
            rootCommand.Description = "ARMory - assesses Azure Resource Manager (ARM) Templates for security and best practice issues.";

            // It has two commands - Assess-template and Assess-directory
            rootCommand.AddCommand(SetupAssessTemplateCommand());

            rootCommand.AddCommand(SetupAssessDirectoryCommand());
            
            return rootCommand;
        }

        private Command SetupAssessTemplateCommand()
        {
            // Setup assess-template 
            Command assessTemplateCommand = new Command("assess-template");

            Option<FileInfo> templateOption = new Option<FileInfo>(
                    "--template-file-path",
                    "The ARM template to assess")
            {
                IsRequired = true
            };
            templateOption.AddAlias("-t");

            assessTemplateCommand.AddOption(templateOption);

            Option<FileInfo> parameterOption = new Option<FileInfo>(
                 "--parameters-file-path",
                 "The parameter file to use when parsing the specified ARM template");
            parameterOption.AddAlias("-p");

            assessTemplateCommand.AddOption(parameterOption);

            assessTemplateCommand.AddOption(SetupOutputFileOption());

            assessTemplateCommand.Handler = CommandHandler.Create<FileInfo, FileInfo>((templateFilePath, parametersFilePath) =>
            {
                try
                {
                    Core.Armory armory = new Core.Armory(File.ReadAllText(templateFilePath.FullName), parametersFilePath == null ? null : File.ReadAllText(parametersFilePath.FullName));
                    IEnumerable<Types.IResult> assessments = armory.EvaluateRulesAgainstTemplate();

                    foreach (var assessment in assessments)
                    {
                        Console.WriteLine($"{assessment.RuleName}: {assessment.RuleDescription}, Result: {assessment.Passed.ToString()}");
                    }
                }
                catch (Exception exp)
                {
                    Console.WriteLine($"An exception occured: {exp.Message}");
                }

            });

            return assessTemplateCommand;
        }

        private Command SetupAssessDirectoryCommand()
        {
            // Setup assess-directory 
            Command assessDirectoryCommand = new Command("assess-directory");

            Option<DirectoryInfo> directoryOption = new Option<DirectoryInfo>(
                    "--directory-path",
                    "Directory to search for ARM templates (.json file extension)")
            {
                IsRequired = true
            };
            directoryOption.AddAlias("-d");

            assessDirectoryCommand.AddOption(directoryOption);

            Option<bool> recursiveOption = new Option<bool>(
                    "--recursive",
                    "Search directory and all subdirectories");
            recursiveOption.AddAlias("-r");

            assessDirectoryCommand.AddOption(recursiveOption);

            assessDirectoryCommand.AddOption(SetupOutputFileOption());

            assessDirectoryCommand.Handler = CommandHandler.Create<DirectoryInfo, bool>((directoryPath, recursive) =>
            {
                // TODO: This needs to call ARMory library and pass in a list of templates
            });

            return assessDirectoryCommand;
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
