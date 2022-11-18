// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    [TestClass]
    public class ConsoleReportWriterTests
    {
        [DataTestMethod]
        [DynamicData("UnitTestCases", typeof(TestCases), DynamicDataSourceType.Property, DynamicDataDisplayName = "GetTestCaseName", DynamicDataDisplayNameDeclaringType = typeof(TestCases))]
        public void WriteResults_Evaluations_ReturnExpectedConsoleLog(string _, MockEvaluation[] evaluations)
        {
            var templateFilePath = new FileInfo(TestCases.TestTemplateFilePath);

            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(evaluations, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, evaluations, templateFilePath);
        }

        private void AssertConsoleLog(StringWriter output, IEnumerable<Types.IEvaluation> testcases, FileInfo templateFilePath)
        {
            var outputString = output.ToString();
            var expected = new StringBuilder();

            var outputResults = new List<List<Result>>();
            var curFile = string.Empty;
            foreach (var evaluation in testcases.Where(e => !e.Passed))
            {
                var distinctFailedResults = evaluation.GetFailedResults().Distinct().ToList();

                // dedupe results
                if (outputResults.Any(results => results.SequenceEqual(distinctFailedResults)))
                {
                    continue;
                }
                outputResults.Add(distinctFailedResults);

                var resultFilePath = distinctFailedResults.First().SourceLocation.FilePath;
                if (curFile != resultFilePath)
                {
                    curFile = resultFilePath;

                    expected.Append($"{Environment.NewLine}{Environment.NewLine}File: {curFile}{Environment.NewLine}");

                    if (resultFilePath != TestCases.TestTemplateFilePath)
                    {
                        expected.Append($"Root Template: {TestCases.TestTemplateFilePath}{Environment.NewLine}");
                    }
                }

                var lineNumbers = distinctFailedResults
                    .Select(r => $"{ConsoleReportWriter.TwiceIndentedNewLine}Line: {r.SourceLocation.LineNumber}")
                    .Aggregate((x, y) => x + y);

                expected.Append($"{ConsoleReportWriter.IndentedNewLine}{(!string.IsNullOrEmpty(evaluation.RuleId) ? $"{evaluation.RuleId}: " : string.Empty)}{evaluation.RuleShortDescription}");
                expected.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}Severity: {evaluation.Severity}");
                if (!string.IsNullOrWhiteSpace(evaluation.Recommendation)) expected.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}Recommendation: {evaluation.Recommendation}");
                expected.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}More information: {evaluation.HelpUri}");
                expected.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}Result: {(evaluation.Passed ? "Passed" : "Failed")} ");
                expected.Append(lineNumbers);
                expected.Append(Environment.NewLine);
            }

            // no failing evals case
            if (curFile == string.Empty)
            {
                expected.Append($"{Environment.NewLine}{Environment.NewLine}File: {TestCases.TestTemplateFilePath}{Environment.NewLine}");
            }

            expected.Append($"{ConsoleReportWriter.IndentedNewLine}Rules passed: {testcases.Count(e => e.Passed)}{Environment.NewLine}");
            outputString.Should().BeEquivalentTo(expected.ToString());
        }
    }
}
