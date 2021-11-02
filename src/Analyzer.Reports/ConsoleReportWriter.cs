// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// Class for output report to console.
    /// </summary>
    public class ConsoleReportWriter : IReportWriter
    {
        private readonly string IndentedNewLine = Environment.NewLine + "\t";
        private readonly string TwiceIndentedNewLine = Environment.NewLine + "\t\t";

        /// <inheritdoc/>
        public void WriteResults(IFileInfo templateFile, IEnumerable<Types.IEvaluation> evaluations)
        {
            OutputToConsole(evaluations);
        }

        private void OutputToConsole(IEnumerable<Types.IEvaluation> evaluations)
        {
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
