// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
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
            string artifactUriString = templateFilePath.Name;
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Rules.Count.Should().Be(1);
            run.Tool.Driver.Rules.First().Id.Should().BeEquivalentTo(ruleId);
            run.OriginalUriBaseIds.Count.Should().Be(1);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(targetDirectory, UriKind.Absolute));
            run.Results.Count.Should().Be(8);
            foreach (Result result in run.Results)
            {
                result.RuleId.Should().BeEquivalentTo(ruleId);
                result.Level.Should().Be(FailureLevel.Error);
                result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo(artifactUriString);
            }
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
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Rules.Count.Should().Be(2);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000022")).Should().Be(true);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000028")).Should().Be(true);
            run.OriginalUriBaseIds.Count.Should().Be(1);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(targetDirectory, UriKind.Absolute));

            run.Results.Count.Should().Be(9);
            foreach (Result result in run.Results)
            {
                if (result.RuleId == "TA-000022")
                {
                    result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo("RedisCache.json");
                    result.Locations.First().PhysicalLocation.ArtifactLocation.UriBaseId.Should().BeEquivalentTo(SarifReportWriter.UriBaseIdString);
                }
                else if (result.RuleId == "TA-000028")
                {
                    result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo("SQLServerAuditingSettings.json");
                    result.Locations.First().PhysicalLocation.ArtifactLocation.UriBaseId.Should().BeEquivalentTo(SarifReportWriter.UriBaseIdString);
                }
                else
                {
                    Assert.Fail("Unexpected result found.");
                }

                result.Level.Should().Be(FailureLevel.Error);
            }
        }

        [TestMethod]
        public void AnalyzeDirectory_WithSubDirectoriesTests()
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

                var templateFilePathInSubFolder = new FileInfo(Path.Combine(targetDirectory, "subfolder", "SQLServerAuditingSettings.json"));
                results = analyzer.AnalyzeTemplate(
                    template: ReadTemplate("SQLServerAuditingSettings.badtemplate"),
                    parameters: null,
                    templateFilePath: templateFilePathInSubFolder.FullName);
                writer.WriteResults(results, (FileInfoBase)templateFilePathInSubFolder);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Rules.Count.Should().Be(2);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000022")).Should().Be(true);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000028")).Should().Be(true);
            run.OriginalUriBaseIds.Count.Should().Be(1);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(targetDirectory, UriKind.Absolute));

            run.Results.Count.Should().Be(9);
            foreach (Result result in run.Results)
            {
                if (result.RuleId == "TA-000022")
                {
                    result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo("RedisCache.json");
                    result.Locations.First().PhysicalLocation.ArtifactLocation.UriBaseId.Should().BeEquivalentTo(SarifReportWriter.UriBaseIdString);
                }
                else if (result.RuleId == "TA-000028")
                {
                    result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo("subfolder/SQLServerAuditingSettings.json");
                    result.Locations.First().PhysicalLocation.ArtifactLocation.UriBaseId.Should().BeEquivalentTo(SarifReportWriter.UriBaseIdString);
                }
                else
                {
                    Assert.Fail("Unexpected result found.");
                }

                result.Level.Should().Be(FailureLevel.Error);
            }
        }

        [TestMethod]
        public void AnalyzeDirectory_TemplateFileNotInRootPathTests()
        {
            // arrange
            string targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "repo");
            string anotherTemplateFilePath = Path.Combine(Directory.GetCurrentDirectory(), "anotherRepo", "subfolder", "SQLServerAuditingSettings.json");
            Uri anotherTemplateFileUri = new Uri(anotherTemplateFilePath);

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

                var templateFilePathInAnotherPath = new FileInfo(anotherTemplateFilePath);
                results = analyzer.AnalyzeTemplate(
                    template: ReadTemplate("SQLServerAuditingSettings.badtemplate"),
                    parameters: null,
                    templateFilePath: templateFilePathInAnotherPath.FullName);
                writer.WriteResults(results, (FileInfoBase)templateFilePathInAnotherPath);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Rules.Count.Should().Be(2);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000022")).Should().Be(true);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000028")).Should().Be(true);
            run.OriginalUriBaseIds.Count.Should().Be(1);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(targetDirectory, UriKind.Absolute));

            run.Results.Count.Should().Be(9);
            foreach (Result result in run.Results)
            {
                if (result.RuleId == "TA-000022")
                {
                    result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo("RedisCache.json");
                    result.Locations.First().PhysicalLocation.ArtifactLocation.UriBaseId.Should().BeEquivalentTo(SarifReportWriter.UriBaseIdString);
                }
                else if (result.RuleId == "TA-000028")
                {
                    // template file is not in scanned directory
                    result.Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo(anotherTemplateFileUri.AbsoluteUri);
                    result.Locations.First().PhysicalLocation.ArtifactLocation.UriBaseId.Should().BeNull();
                }
                else
                {
                    Assert.Fail("Unexpected result found.");
                }

                result.Level.Should().Be(FailureLevel.Error);
            }
        }

        private string ReadTemplate(string templateFileName)
        {
            return File.ReadAllText(Path.Combine("TestTemplates", templateFileName));
        }

        private SarifReportWriter SetupWriter(Stream stream, string targetDirectory = null)
        {
            var mockFileSystem = new Mock<IFileInfo>();
            mockFileSystem
                .Setup(x => x.Create())
                .Returns(() => stream);
            return new SarifReportWriter(mockFileSystem.Object, targetDirectory);
        }
    }
}
