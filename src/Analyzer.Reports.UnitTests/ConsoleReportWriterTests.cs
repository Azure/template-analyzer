﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(evaluations, (FileInfoBase)new FileInfo(TestCases.TestTemplateFilePath));
            }

            // assert
            AssertConsoleLog(output, evaluations);
        }

        private void AssertConsoleLog(StringWriter output, IEnumerable<Types.IEvaluation> testcases)
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
                    var extraNewLine = (curFile == string.Empty) ? string.Empty : Environment.NewLine;
                    curFile = resultFilePath;
                    
                    expected.Append($"{Environment.NewLine}{Environment.NewLine}{extraNewLine}Template: {curFile}");

                    if (curFile != TestCases.TestTemplateFilePath)
                    {
                        expected.Append($"{ConsoleReportWriter.IndentedNewLine}Root Template: {TestCases.TestTemplateFilePath}");
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
            }

            // No failing evals case
            if (curFile == string.Empty)
            {
                expected.Append($"{Environment.NewLine}{Environment.NewLine}Template: {TestCases.TestTemplateFilePath}");
            }

            expected.Append($"{ConsoleReportWriter.IndentedNewLine}Rules passed: {testcases.Count(e => e.Passed)}{Environment.NewLine}");
            outputString.Should().BeEquivalentTo(expected.ToString());
        }
    }
}
