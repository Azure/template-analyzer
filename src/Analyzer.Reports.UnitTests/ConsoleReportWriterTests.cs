// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    [TestClass]
    public class ConsoleReportWriterTests
    {
        [DataTestMethod]
        [DynamicData("UnitTestCases", typeof(TestCases), DynamicDataSourceType.Property, DynamicDataDisplayName = "GetTestCaseName", DynamicDataDisplayNameDeclaringType = typeof(TestCases))]
        public void WriteResults_Evalutions_ReturnExpectedSarifLog(string _, MockEvaluation[] evaluations)
        {
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");

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
            string outputString = output.ToString();
            var expected = new StringBuilder();
            expected.Append($"{Environment.NewLine}{Environment.NewLine}File: {templateFilePath}{Environment.NewLine}");

            foreach (var evaluation in testcases.Where(e => !e.Passed))
            {
                expected.Append($"{ConsoleReportWriter.IndentedNewLine}{(!string.IsNullOrEmpty(evaluation.RuleId) ? $"{evaluation.RuleId}: " : string.Empty)}{evaluation.RuleShortDescription}");
                expected.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}Severity: {evaluation.Severity}");
                if (!string.IsNullOrWhiteSpace(evaluation.Recommendation)) expected.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}Recommendation: {evaluation.Recommendation}");
                expected.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}More information: {evaluation.HelpUri}");
                expected.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}Result: {(evaluation.Passed ? "Passed" : "Failed")} ");
                expected.Append(GetLineNumbers(evaluation));
                expected.Append(Environment.NewLine);
            }
            expected.Append($"{ConsoleReportWriter.IndentedNewLine}Rules passed: {testcases.Count(e => e.Passed)}{Environment.NewLine}");
            outputString.Should().BeEquivalentTo(expected.ToString());
        }

        private string GetLineNumbers(Types.IEvaluation evaluation, HashSet<int> failedLines = null)
        {
            failedLines ??= new HashSet<int>();
            var resultString = new StringBuilder();
            if (!evaluation.Passed)
            {
                if ((!evaluation.Result?.Passed ?? false) && !failedLines.Any(l => l == evaluation.Result.LineNumber))
                {
                    failedLines.Add(evaluation.Result.LineNumber);
                    resultString.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}Line: {evaluation.Result.LineNumber}");
                }

                foreach (var innerEvaluation in evaluation.Evaluations)
                {
                    resultString.Append(GetLineNumbers(innerEvaluation, failedLines));
                }
            }
            return resultString.ToString();
        }
    }
}
