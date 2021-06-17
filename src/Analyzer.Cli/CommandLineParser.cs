// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    internal class CommandLineParser
    {
        RootCommand rootCommand;
        private readonly string IndentedNewLine = Environment.NewLine + "\t";
        private readonly string TwiceIndentedNewLine = Environment.NewLine + "\t\t";

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

            rootCommand.AddCommand(SetupAnalyzeTemplateCommand());
            
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

            analyzeTemplateCommand.Handler = CommandHandler.Create<FileInfo, FileInfo>((templateFilePath, parametersFilePath) =>
            {
                try
                {
                    Core.TemplateAnalyzer templateAnalyzer = new Core.TemplateAnalyzer(File.ReadAllText(templateFilePath.FullName), parametersFilePath == null ? null : File.ReadAllText(parametersFilePath.FullName), templateFilePath.FullName);
                    IEnumerable<Types.IEvaluation> evaluations = templateAnalyzer.EvaluateRulesAgainstTemplate();

                    string fileMetadata = Environment.NewLine + Environment.NewLine + $"File: {templateFilePath}";
                    if (parametersFilePath != null)
                    {
                        fileMetadata += Environment.NewLine + $"Parameters File: {parametersFilePath}";
                    }

                    Console.WriteLine(fileMetadata);

                    var passedEvaluations = 0;

                    foreach (var evaluation in evaluations)
                    {
                        string resultString = GenerateResultString(evaluation);
                        
                        if (!evaluation.Passed)
                        {
                            var output = $"{IndentedNewLine}{evaluation.RuleName}: {evaluation.RuleDescription}" +
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
                foreach (var result in evaluation.Results)
                {
                    resultString += $"{TwiceIndentedNewLine}Line: {result.LineNumber}";
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
