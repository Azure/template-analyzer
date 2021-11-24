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
        [TestMethod]
        public void WriteResults_Evalutions_ReturnExpectedSarifLog()
        {
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            foreach (var evaluations in TestCases.UnitTestCases)
            {
                var output = new StringWriter();
                Console.SetOut(output);
                using (var writer = new ConsoleReportWriter())
                {
                    writer.WriteResults(evaluations, (FileInfoBase)templateFilePath);
                }

                // assert
                AssertConsoleLog(output, evaluations, templateFilePath);
            }
        }

        private void AssertConsoleLog(StringWriter output, IEnumerable<Types.IEvaluation> testcases, FileInfo templateFilePath)
        {
            string outputString = output.ToString();
            var expected = new StringBuilder();
            expected.Append($"{Environment.NewLine}{Environment.NewLine}File: {templateFilePath}{Environment.NewLine}");

            foreach (var evaluation in testcases.Where(e => !e.Passed))
            {
                expected.Append($"{ConsoleReportWriter.IndentedNewLine}{(!string.IsNullOrEmpty(evaluation.RuleId) ? $"{evaluation.RuleId}: " : string.Empty)}{evaluation.RuleDescription}");
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

        private string GetLineNumbers(Types.IEvaluation evaluation)
        {
            var resultString = new StringBuilder();
            if (!evaluation.Passed)
            {
                foreach (var result in evaluation.Results.Where(r => !r.Passed))
                {
                    resultString.Append($"{ConsoleReportWriter.TwiceIndentedNewLine}Line: {result.LineNumber}");
                }

                foreach (var innerEvaluation in evaluation.Evaluations)
                {
                    resultString.Append(GetLineNumbers(innerEvaluation));
                }
            }
            return resultString.ToString();
        }
    }
}
