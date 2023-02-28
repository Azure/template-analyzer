// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// An <see cref="IReportWriter"/> for writing results to the console.
    /// </summary>
    public class ConsoleReportWriter : IReportWriter
    {
        internal static string IndentedNewLine = Environment.NewLine + "\t";
        internal static string TwiceIndentedNewLine = Environment.NewLine + "\t\t";

        private readonly List<string> filesAlreadyOutput = new List<string>();

        /// <inheritdoc/>
        public void WriteResults(IEnumerable<Types.IEvaluation> evaluations, IFileInfo templateFile, IFileInfo parametersFile = null)
        {
            var resultsByFile = ReportsHelper.GetResultsByFile(evaluations, filesAlreadyOutput, out int passedEvaluations);

            // output files in sorted order, but always output root first and regardless if no results
            var filesWithResults = resultsByFile.Keys.ToList();
            var removed = filesWithResults.Remove(templateFile.FullName);
            filesWithResults.Sort();
            if (removed) filesWithResults.Insert(0, templateFile.FullName);

            foreach (var fileWithResults in filesWithResults)
            {
                var fileMetadata = $"{Environment.NewLine}{Environment.NewLine}Template: {fileWithResults}";

                if (fileWithResults == templateFile.FullName)
                {
                    if (parametersFile != null)
                    {
                        fileMetadata += $"{IndentedNewLine}Parameters File: {parametersFile.FullName}";
                    }
                }
                else
                {
                    fileMetadata += $"{IndentedNewLine}Root Template: {templateFile.FullName}";
                }

                Console.WriteLine(fileMetadata);

                foreach ((var evaluation, var failedResults) in resultsByFile[fileWithResults])
                {
                    string resultString = string.Concat(failedResults.Select(result => $"{TwiceIndentedNewLine}Line: {result.SourceLocation.LineNumber}"));
                    var output = $"\t{(evaluation.RuleId != "" ? $"{evaluation.RuleId}: " : "")}{evaluation.RuleShortDescription}" +
                        $"{TwiceIndentedNewLine}Severity: {evaluation.Severity}" + 
                        (!string.IsNullOrWhiteSpace(evaluation.Recommendation) ? $"{TwiceIndentedNewLine}Recommendation: {evaluation.Recommendation}" : "") +
                        $"{TwiceIndentedNewLine}More information: {evaluation.HelpUri}" +
                        $"{TwiceIndentedNewLine}Result: {(evaluation.Passed ? "Passed" : "Failed")} {resultString}";
                    Console.WriteLine(output);
                }
            }

            // ensure filename output if there were no failed results
            if (filesWithResults.Count() == 0)
            {
                var fileMetadata = $"{Environment.NewLine}{Environment.NewLine}Template: {templateFile.FullName}";
                if (parametersFile != null)
                {
                    fileMetadata += $"{Environment.NewLine}Parameters File: {parametersFile.FullName}";
                }
                Console.WriteLine(fileMetadata);
            }

            filesAlreadyOutput.AddRange(filesWithResults);

            Console.WriteLine($"\tRules passed: {passedEvaluations}");
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}
