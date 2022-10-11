// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

using TAResult = Microsoft.Azure.Templates.Analyzer.Types.Result;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    [TestClass]
    public class SarifReportWriterTests
    {
        [DataTestMethod]
        [DynamicData("UnitTestCases", typeof(TestCases), DynamicDataSourceType.Property, DynamicDataDisplayName = "GetTestCaseName", DynamicDataDisplayNameDeclaringType = typeof(TestCases))]
        public void WriteResults_Evalutions_ReturnExpectedSarifLog(string _, MockEvaluation[] evaluations)
        {
            var templateFilePath = new FileInfo(TestCases.TestTemplateFilePath);

            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults(evaluations, (FileInfoBase)templateFilePath);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(Encoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, evaluations, templateFilePath);
        }

        [TestMethod]
        public void IsSubPath_Tests()
        {
            var rootDir = Path.GetTempPath();
            var isCaseSensitive = IsFileSystemCaseSensitive();
            var testCases = new[]
            {
                new { FilePath = Path.Combine(rootDir, "foo", "bar", "config.json"), RootPath = Path.Combine(rootDir, "foo"), Expected = true },
                new { FilePath = Path.Combine(rootDir, "foo", "bar", "level2", "config.json"), RootPath = Path.Combine(rootDir, "foo"), Expected = true },
                new { FilePath = Path.Combine(rootDir, "foo", "config.json"), RootPath = Path.Combine(rootDir, "foo", "bar"), Expected = false },
                new { FilePath = Path.Combine(rootDir, "foo", "bar", "config.json"), RootPath = Path.Combine(rootDir, "foo", ""), Expected = true },
                new { FilePath = Path.Combine(rootDir, "anotherPath", "foo", "bar", "config.json"), RootPath = Path.Combine(rootDir, "foo"), Expected = false },
                new { FilePath = Path.Combine(rootDir, "FOO", "BAR", "config.json"), RootPath = Path.Combine(rootDir, "foo"), Expected = !isCaseSensitive },
                new { FilePath = Path.Combine(rootDir, "foo", "bar", "config.json"), RootPath = Path.Combine(rootDir, "foo", "BAR"), Expected = !isCaseSensitive },
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

        private int GetResultsCount(IEnumerable<Types.IEvaluation> evaluations)
        {
            int count = 0;
            foreach (var eval in evaluations.Where(e => !e.Passed))
            {
                count += eval.Result?.Passed ?? true ? 0 : 1;
                count += GetResultsCount(eval.Evaluations);
            }

            return count;
        }

        private void AssertSarifLog(SarifLog sarifLog, IEnumerable<Types.IEvaluation> evaluations, FileInfo templateFilePath)
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
            var failedEvaluations = evaluations.Where(e => !e.Passed).ToList();
            int expectedRuleCount = failedEvaluations
                .Select(e => e.RuleId)
                .Distinct()
                .Count();
            int resultCount = GetResultsCount(evaluations);
            if (expectedRuleCount == 0 || resultCount == 0)
            {
                rules.Should().BeNull();
                run.Results.Should().BeNull();
            }
            else
            {
                rules.Count.Should().Be(expectedRuleCount);
                foreach (var evaluation in evaluations)
                {
                    var rule = rules.SingleOrDefault(r => r.Id.Equals(evaluation.RuleId));
                    if (evaluation.Passed)
                    {
                        rule.Should().BeNull();
                    }
                    else
                    {
                        rule.Should().NotBeNull();
                        rule.Id.Should().BeEquivalentTo(evaluation.RuleId);
                        rule.FullDescription.Text.Should().BeEquivalentTo(SarifReportWriter.AppendPeriod(evaluation.RuleDescription));
                        rule.Help.Text.Should().BeEquivalentTo(SarifReportWriter.AppendPeriod(evaluation.Recommendation));
                        rule.HelpUri.OriginalString.Should().BeEquivalentTo(evaluation.HelpUri);
                    }
                }

                var outputResults = new List<List<TAResult>>();
                for (int i = 0; i < failedEvaluations.Count; i++)
                {
                    var evaluation = failedEvaluations[i];
                    var evalResults = ReportsHelper.GetFailedResultsAsList(evaluation).Distinct().ToList();

                    // dedupe results
                    if (outputResults.Any(results => results.SequenceEqual(evalResults)))
                    {
                        continue;
                    }

                    var result = run.Results[outputResults.Count];
                    result.RuleId.Should().BeEquivalentTo(evaluation.RuleId);
                    result.Message.Id.Should().BeEquivalentTo("default");
                    result.Level.Should().Be(FailureLevel.Error);

                    if (evalResults.First().SourceLocation.FilePath != templateFilePath.FullName)
                    {
                        result.AnalysisTarget.Uri.OriginalString.Should().BeEquivalentTo(templateFilePath.Name);
                    }
                    else
                    {
                        result.AnalysisTarget.Should().BeNull();
                    }

                    for (int j = 0; j < evalResults.Count; j++)
                    {
                        var evalResultFileName = Path.GetFileName(evalResults[j].SourceLocation.FilePath);
                        result.Locations[j].PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo(evalResultFileName);
                        result.Locations[j].PhysicalLocation.Region.StartLine.Should().Be(evalResults[j].SourceLocation.LineNumber);
                    }

                    outputResults.Add(evalResults);
                }

                run.Results.Count.Should().Be(outputResults.Count);
            }
        }

        private static bool IsFileSystemCaseSensitive()
        {
            var tmp = Path.GetTempPath();
            return !Directory.Exists(tmp.ToUpper()) || !Directory.Exists(tmp.ToLower());
        }
    }
}
