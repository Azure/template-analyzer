// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Azure.Deployments.Core.Extensions;
using FluentAssertions;
using Microsoft.Azure.Templates.Analyzer.Core;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    [TestClass]
    public class SarifReportWriterE2ETests
    {
        [TestMethod]
        public void AnalyzeTemplateTests()
        {
            // arrange
            string targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Azure");
            var templateFilePath = new FileInfo(Path.Combine(targetDirectory, "SQLServerAuditingSettings.json"));

            var results = TemplateAnalyzer.Create().AnalyzeTemplate(
                template: ReadTemplate("SQLServerAuditingSettings.badtemplate"),
                parameters: null,
                templateFilePath: templateFilePath.FullName);

            // act
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults(results, (FileInfoBase)templateFilePath);
            }

            // assert
            string ruleId = "TA-000028";
            List<List<int>> expectedLinesForRun = new List<List<int>>
            {
                new List<int> { 146, 147, 148, 148 },
                new List<int> { 206, 207, 208, 208 }
            };

            string artifactUriString = templateFilePath.Name;
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(Encoding.UTF8.GetString(memStream.ToArray()));
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Rules.Count.Should().Be(1);
            run.Tool.Driver.Rules.First().Id.Should().BeEquivalentTo(ruleId);
            run.OriginalUriBaseIds.Count.Should().Be(1);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(targetDirectory, UriKind.Absolute));
            run.Results.Count.Should().Be(expectedLinesForRun.Count);

            foreach (Result result in run.Results)
            {
                result.RuleId.Should().BeEquivalentTo(ruleId);
                result.Level.Should().Be(FailureLevel.Error);

                var expectedLines = expectedLinesForRun.FirstOrDefault(l => l.Contains(result.Locations.First().PhysicalLocation.Region.StartLine));
                expectedLines.Should().NotBeNull("There shouldn't be a line number reported outside of the expected lines.");
                expectedLinesForRun.Remove(expectedLines);

                // Verify lines reported equal the expected lines
                result.Locations.Count.Should().Be(expectedLines.Count);
                foreach (var location in result.Locations)
                {
                    location.PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo(artifactUriString);
                    var line = location.PhysicalLocation.Region.StartLine;

                    // Verify line is expected, and remove from the collection
                    expectedLines.Contains(line).Should().BeTrue();
                    expectedLines.Remove(line);
                }

                // Verify all lines were reported
                expectedLines.Should().BeEmpty();
            }

            // Verify all lines were reported
            expectedLinesForRun.Should().BeEmpty();
        }

        [TestMethod]
        public void AnalyzeDirectoryTests()
        {
            // arrange
            string targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "repo");

            // act
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream, targetDirectory))
            {
                var analyzer = TemplateAnalyzer.Create();
                var templateFilePath = new FileInfo(Path.Combine(targetDirectory, "RedisCache.json"));
                var results = analyzer.AnalyzeTemplate(
                    template: ReadTemplate("RedisCache.badtemplate"),
                    parameters: null,
                    templateFilePath: templateFilePath.FullName);
                writer.WriteResults(results, (FileInfoBase)templateFilePath);

                templateFilePath = new FileInfo(Path.Combine(targetDirectory, "SQLServerAuditingSettings.json"));
                results = analyzer.AnalyzeTemplate(
                    template: ReadTemplate("SQLServerAuditingSettings.badtemplate"),
                    parameters: null,
                    templateFilePath: templateFilePath.FullName);
                writer.WriteResults(results, (FileInfoBase)templateFilePath);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(Encoding.UTF8.GetString(memStream.ToArray()));
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Rules.Count.Should().Be(2);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000022")).Should().Be(true);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000028")).Should().Be(true);
            run.OriginalUriBaseIds.Count.Should().Be(1);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(targetDirectory, UriKind.Absolute));

            var expectedLinesForRun = new Dictionary<string, (string file, List<List<int>> lines)>
            {
                { "TA-000022", ("RedisCache.json", new List<List<int>> {
                    new List<int> { 28 } })
                },
                { "TA-000028", ("SQLServerAuditingSettings.json", new List<List<int>> {
                    new List<int> { 146, 147, 148, 148 },
                    new List<int> { 206, 207, 208, 208 } })
                }
            };

            run.Results.Count.Should().Be(3);
            foreach (Result result in run.Results)
            {
                expectedLinesForRun.ContainsKey(result.RuleId).Should().BeTrue("Unexpected result found in SARIF");

                result.Locations.Count.Should().BeGreaterThan(0);

                // Find correct set of lines expected for result
                (var fileName, var linesForResult) = expectedLinesForRun[result.RuleId];
                var expectedLines = linesForResult.FirstOrDefault(l => l.Contains(result.Locations.First().PhysicalLocation.Region.StartLine));
                expectedLines.Should().NotBeNull("There shouldn't be a line number reported outside of the expected lines.");
                linesForResult.Remove(expectedLines);

                // Verify lines reported equal the expected lines
                result.Locations.Count.Should().Be(expectedLines.Count);
                foreach (var location in result.Locations)
                {
                    location.PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo(fileName);
                    var line = location.PhysicalLocation.Region.StartLine;

                    // Verify line is expected, and remove from the collection
                    expectedLines.Contains(line).Should().BeTrue();
                    expectedLines.Remove(line);
                }

                // Verify all lines were reported
                expectedLines.Should().BeEmpty();

                // Remove record for rule if all lines have been reported
                if (linesForResult.Count == 0)
                {
                    expectedLinesForRun.Remove(result.RuleId);
                }

                result.Level.Should().Be(FailureLevel.Error);
            }

            // Verify all lines and results were reported
            expectedLinesForRun.Should().BeEmpty();
        }

        private static string ReadTemplate(string templateFileName)
        {
            return File.ReadAllText(Path.Combine("TestTemplates", templateFileName));
        }

        private static SarifReportWriter SetupWriter(Stream stream, string targetDirectory = null)
        {
            var mockFileSystem = new Mock<IFileInfo>();
            mockFileSystem
                .Setup(x => x.Create())
                .Returns(() => stream);
            return new SarifReportWriter(mockFileSystem.Object, targetDirectory);
        }
    }
}
