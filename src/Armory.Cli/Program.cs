using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Armory.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            return SetupCommandLineAPI().InvokeAsync(args).Result;
        }

        private static RootCommand SetupCommandLineAPI()
        {
            // Create a root command 
            var rootCommand = new RootCommand();
            rootCommand.Description = "ARMory - assesses Azure Resource Manager (ARM) Templates for security and best practice issues.";

            // It has two commands - Assess-template and Assess-directory
            rootCommand.AddCommand(SetupAssessTemplateCommand());

            rootCommand.AddCommand(SetupAssessDirectoryCommand());

            return rootCommand;
        }

        private static Command SetupAssessTemplateCommand()
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
                //Console.WriteLine($"The value for --template-file-path is: {templateFilePath?.FullName ?? "null"}");
                //Console.WriteLine($"The value for --parameters-file-path is: {parametersFilePath?.FullName ?? "null"}");

                Core.Armory armory = new Core.Armory(templateFilePath.FullName);
                IEnumerable<Types.IResult> assessments = armory.EvaluateRulesAgainstTemplate();

                foreach (var assessment in assessments)
                {
                    Console.WriteLine($"{assessment.RuleName} at {assessment.LineNumber}");
                }

            });

            return assessTemplateCommand;
        }

        private static Command SetupAssessDirectoryCommand()
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
                Console.WriteLine($"The value for --directory-path is: {directoryPath?.FullName ?? "null"}");
                Console.WriteLine($"The value for --recursive is: {recursive}");

                // TODO: This needs to call ARMory library
            });

            return assessDirectoryCommand;
        }

        private static Option SetupOutputFileOption()
        {
            Option<FileInfo> outputFileOption = new Option<FileInfo>(
                        "--output-path",
                        "Redirect output to specified file in JSON format");

            outputFileOption.AddAlias("-o");

            return outputFileOption;
        }
    }
}
