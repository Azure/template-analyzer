// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// An <see cref="IReportWriter"/> for writing results to the console.
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
                    string resultString = string.Concat(
                        GetFailedLines(evaluation)
                        .Select(l => $"{TwiceIndentedNewLine}Line: {l}"));
                    var output = $"{IndentedNewLine}{(evaluation.RuleId != "" ? $"{evaluation.RuleId}: " : "")}{evaluation.RuleDescription}" +
                        $"{TwiceIndentedNewLine}Severity: {evaluation.Severity}" + 
                        (!string.IsNullOrWhiteSpace(evaluation.Recommendation) ? $"{TwiceIndentedNewLine}Recommendation: {evaluation.Recommendation}" : "") +
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

        private HashSet<int> GetFailedLines(IEvaluation evaluation, HashSet<int> failedLines = null)
        {
            failedLines ??= new HashSet<int>();

            if (!evaluation.Result?.Passed ?? false)
            {
                failedLines.Add(evaluation.Result.LineNumber);
            }

            foreach(var eval in evaluation.Evaluations.Where(e => !e.Passed))
            {
                GetFailedLines(eval, failedLines);
            }

            return failedLines;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
