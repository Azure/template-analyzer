// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    [TestClass]
    public class SarifReportWriterTests
    {
        [TestMethod]
        public void WriteResults_Evalutions_ReturnExpectedSarifLog()
        {
            string currentFolder = Path.Combine(Directory.GetCurrentDirectory(), "testRepo");
            var templateFilePath = new FileInfo(Path.Combine(currentFolder, "AppServices.json"));
            foreach (var evaluations in TestCases.UnitTestCases)
            {
                var memStream = new MemoryStream();
                using (var writer = SetupWriter(memStream))
                {
                    writer.WriteResults(evaluations, (FileInfoBase)templateFilePath);
                }

                // assert
                SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
                AssertSarifLog(sarifLog, evaluations, templateFilePath);
            }
        }

        [TestMethod]
        public void IsSubPath_Tests()
        {
            var rootDir = Path.GetTempPath();
            var testCases = new[]
            {
                new { FilePath = Path.Combine(rootDir, "foo", "bar", "config.json"), RootPath = Path.Combine(rootDir, "foo"), Expected = true },
                new { FilePath = Path.Combine(rootDir, "foo", "bar", "level2", "config.json"), RootPath = Path.Combine(rootDir, "foo"), Expected = true },
                new { FilePath = Path.Combine(rootDir, "foo", "config.json"), RootPath = Path.Combine(rootDir, "foo", "bar"), Expected = false },
                new { FilePath = Path.Combine(rootDir, "foo", "bar", "config.json"), RootPath = Path.Combine(rootDir, "foo", ""), Expected = true },
                new { FilePath = Path.Combine(rootDir, "anotherPath", "foo", "bar", "config.json"), RootPath = Path.Combine(rootDir, "foo"), Expected = false },
                new { FilePath = "https://example.com/config.json", RootPath = Path.Combine(rootDir, "foo"), Expected = false },
                new { FilePath = @"\\hostname\share\config.json", RootPath = Path.Combine(rootDir, "foo"), Expected = false },
            };

            foreach (var testCase in testCases)
            {
                bool result = SarifReportWriter.IsSubPath(testCase.RootPath, testCase.FilePath);
                result.Should().Be(testCase.Expected);
            }
        }

        private SarifReportWriter SetupWriter(Stream stream)
        {
            var mockFileSystem = new Mock<IFileInfo>();
            mockFileSystem
                .Setup(x => x.Create())
                .Returns(() => stream);
            return new SarifReportWriter(mockFileSystem.Object);
        }

        private void TraverseResults(IList<Types.IResult> results, Types.IEvaluation evaluation)
        {
            foreach (var result in evaluation.Results.Where(r => !r.Passed))
            {
                results.Add(result);
            }
            foreach (var child in evaluation.Evaluations.Where(r => !r.Passed))
            {
                TraverseResults(results, child);
            }
        }

        private int GetResultsCount(IEnumerable<Types.IEvaluation> evaluations)
        {
            int count = 0;
            foreach (var eval in evaluations.Where(r => !r.Passed))
            {
                count += eval.Results.Count(r => !r.Passed);
                count += GetResultsCount(eval.Evaluations);
            }

            return count;
        }

        private void AssertSarifLog(SarifLog sarifLog, IEnumerable<Types.IEvaluation> testcases, FileInfo templateFilePath)
        {
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Name.Should().BeEquivalentTo(Constants.ToolName);
            run.Tool.Driver.FullName.Should().BeEquivalentTo(Constants.ToolFullName);
            run.Tool.Driver.Version.Should().BeEquivalentTo(Constants.ToolVersion);
            run.Tool.Driver.Organization.Should().BeEquivalentTo(Constants.Organization);
            run.Tool.Driver.InformationUri.OriginalString.Should().BeEquivalentTo(Constants.InformationUri);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(templateFilePath.DirectoryName, UriKind.Absolute));

            IList<ReportingDescriptor> rules = run.Tool.Driver.Rules;
            int ruleCount = testcases.Count(t => !t.Passed);
            int resultCount = GetResultsCount(testcases);
            if (ruleCount == 0 || resultCount == 0)
            {
                rules.Should().BeNull();
            }
            else
            {
                rules.Count.Should().Be(ruleCount);
                foreach (var testcase in testcases)
                {
                    var rule = rules.FirstOrDefault(r => r.Id.Equals(testcase.RuleId));
                    rule.Should().NotBeNull();
                    rule.Id.Should().BeEquivalentTo(testcase.RuleId);
                    rule.FullDescription.Text.Should().BeEquivalentTo(SarifReportWriter.AppendPeriod(testcase.RuleDescription));
                    rule.Help.Text.Should().BeEquivalentTo(SarifReportWriter.AppendPeriod(testcase.Recommendation));
                    rule.HelpUri.OriginalString.Should().BeEquivalentTo(testcase.HelpUri);
                }
            }

            IList<Result> results = run.Results;
            int i = 0;
            foreach (var testcase in testcases)
            {
                var evalResults = new List<Types.IResult>();
                TraverseResults(evalResults, testcase);
                foreach (var res in evalResults)
                {
                    results[i].RuleId.Should().BeEquivalentTo(testcase.RuleId);
                    results[i].Message.Id.Should().BeEquivalentTo("default");
                    results[i].Level = res.Passed ? FailureLevel.Note : FailureLevel.Error;
                    results[i].Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo(templateFilePath.Name);
                    results[i].Locations.First().PhysicalLocation.Region.StartLine.Should().Be(res.LineNumber);
                    i++;
                }
            }
        }

    }
}
