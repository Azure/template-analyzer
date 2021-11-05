// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO.Abstractions;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// Class for output report to console.
    /// </summary>
    public class ConsoleReportWriter : IReportWriter
    {
        internal static string IndentedNewLine = Environment.NewLine + "\t";
        internal static string TwiceIndentedNewLine = Environment.NewLine + "\t\t";

        /// <inheritdoc/>
        public void WriteResults(IEnumerable<Types.IEvaluation> evaluations, IFileInfo templateFile, IFileInfo parametersFile = null)
        {
            // Log info on file to be analyzed
            string fileMetadata = Environment.NewLine + Environment.NewLine + $"File: {templateFile}";
            if (parametersFile != null)
            {
                fileMetadata += Environment.NewLine + $"Parameters File: {parametersFile}";
            }
            Console.WriteLine(fileMetadata);

            OutputToConsole(evaluations);
        }

        private void OutputToConsole(IEnumerable<Types.IEvaluation> evaluations)
        {
            var passedEvaluations = 0;

            foreach (var evaluation in evaluations)
            {
                if (!evaluation.Passed)
                {
                    string resultString = GenerateResultString(evaluation);
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

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
