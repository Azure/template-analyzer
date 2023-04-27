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
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    [TestClass]
    public class SarifReportWriterE2ETests
    {
        [TestMethod]
        [DataRow("SQLServerAuditingSettings.json")]
        [DataRow("SQLServerAuditingSettings.bicep")]
        [DataRow("TemplateWithJsonReference.bicep", "SQLServerAuditingSettings.json")]
        [DataRow("TemplateWithBicepReference.bicep", "SQLServerAuditingSettings.bicep")]
        public void AnalyzeTemplateTests(string template, string referencedTemplate = null)
        {
            var isBicepResult = referencedTemplate == null
                ? template.EndsWith(".bicep")
                : referencedTemplate.EndsWith(".bicep");

            // arrange
            string targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestTemplates");
            var templateFilePath = new FileInfo(Path.Combine(targetDirectory, template));

            var results = TemplateAnalyzer.Create(false).AnalyzeTemplate(
                template: ReadTemplate(template),
                parameters: null,
                templateFilePath: templateFilePath.FullName);

            // act
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults(results, (FileInfoBase)templateFilePath);
            }

            // assert
            var expectedLinesForRun = new Dictionary<string, List<List<int>>>
            {
                { "TA-000028", new List<List<int>> {
                        isBicepResult ? new List<int> { 14, 15, 16 } : new List<int> { 23, 24, 25 },
                        isBicepResult ? new List<int> { 31, 32, 33 } : new List<int> { 43, 44, 45 }
                    }
                },
                { "AZR-000186", new List<List<int>> {
                        new List<int> { 1 }
                    }
                },
                { "AZR-000187", new List<List<int>> {
                        isBicepResult ? new List<int> { 5 } : new List<int> { 14 },
                        isBicepResult ? new List<int> { 22 } : new List<int> { 34 }
                    }
                },
                { "AZR-000188", new List<List<int>> {
                        isBicepResult ? new List<int> { 5 } : new List<int> { 14 },
                        isBicepResult ? new List<int> { 22 } : new List<int> { 34 }
                    }
                },
                { "AZR-000189", new List<List<int>> {
                        isBicepResult ? new List<int> { 5 } : new List<int> { 14 },
                        isBicepResult ? new List<int> { 22 } : new List<int> { 34 }
                    }
                }
            };

            string artifactUriString = templateFilePath.Name;
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(Encoding.UTF8.GetString(memStream.ToArray()));
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Rules.Count.Should().Be(5);
            run.OriginalUriBaseIds.Count.Should().Be(1);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(targetDirectory, UriKind.Absolute));
            run.Results.Count.Should().Be(9);

            foreach (Result result in run.Results)
            {
                expectedLinesForRun.ContainsKey(result.RuleId).Should().BeTrue("Unexpected result found in SARIF");
                result.Level.Should().Be(GetLevelFromSeverity(results.First(r => result.RuleId == r.RuleId).Severity));

                // Determine which template file was evaluated for this SARIF result (all locations will be in same file, so taking first)
                // depending on if eval file matches the original target file, verify if analysis target is present or not
                var distinctEvalFiles = result.Locations.Select(loc => loc.PhysicalLocation.ArtifactLocation.Uri.OriginalString).Distinct().ToList();

                if (distinctEvalFiles.Count != 1) throw new Exception("Multiple eval files for single SARIF result");

                string evalFile = null;
                if (referencedTemplate != null && result.RuleId != "AZR-000186") // special case for rule, not in referenced template regardless
                {
                    evalFile = referencedTemplate;
                    result.AnalysisTarget.Uri.OriginalString.Should().BeEquivalentTo(artifactUriString);
                }
                else
                {
                    evalFile = artifactUriString;
                    result.AnalysisTarget.Should().BeNull();
                }

                var linesForResult = expectedLinesForRun[result.RuleId];
                var expectedLines = linesForResult.FirstOrDefault(l => l.Contains(result.Locations.First().PhysicalLocation.Region.StartLine));
                expectedLines.Should().NotBeNull("There shouldn't be a line number reported outside of the expected lines.");
                linesForResult.Remove(expectedLines);

                // Verify lines reported equal the expected lines
                result.Locations.Count.Should().Be(expectedLines.Count);
                foreach (var location in result.Locations)
                {
                    location.PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo(evalFile);

                    // Verify line is expected, and remove from the collection
                    var line = location.PhysicalLocation.Region.StartLine;
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
            }

            // Verify all lines were reported
            expectedLinesForRun.Should().BeEmpty();
        }

        [DataTestMethod]
        [DataRow("RedisCache.json", "SQLServerAuditingSettings.json", null, DisplayName = "Templates in root directory")]
        [DataRow("RedisCache.json", "SQLServerAuditingSettings.json", null, "subPath", DisplayName = "Template in subdirectory")]
        [DataRow("RedisCache.json", "SQLServerAuditingSettings.json", null, "..", "anotherRepo", "subfolder", DisplayName = "Templates under root directory and outside of root")]
        [DataRow("RedisCache.bicep", "RedisAndSQL.bicep", "SQLServerAuditingSettings.bicep", DisplayName = "RedisCache template results have multiple references, file results should be deduped")]
        public void AnalyzeDirectoryTests(string firstTemplate, string secondTemplate, string nestedTemplate, params string[] secondTemplatePathPieces)
        {
            var secondTemplateUsesRelativePath = !secondTemplatePathPieces.Contains("..");

            // arrange
            var targetDirectory = Path.Combine(Path.GetTempPath(), "repo");

            var firstTemplatePath = Path.Combine(targetDirectory, firstTemplate);
            var firstTemplateFileInfo = new FileInfo(firstTemplatePath);
            var firstTemplateString = ReadTemplate(firstTemplate);

            var secondTemplateDirectory = Path.Combine(secondTemplatePathPieces.Prepend(targetDirectory).ToArray());
            var secondTemplatePath = Path.Combine(secondTemplateDirectory, secondTemplate);
            var secondTemplateString = ReadTemplate(secondTemplate);
            var secondTemplateFileInfo = new FileInfo(secondTemplatePath);
            var expectedSecondTemplateFilePathInSarif = secondTemplateUsesRelativePath
                ? UriHelper.MakeValidUri(Path.Combine(secondTemplatePathPieces.Append(secondTemplate).ToArray()))
                : new Uri(secondTemplatePath).AbsoluteUri;

            // act
            var memStream = new MemoryStream();
            List<Types.IEvaluation> analyzerResults = null;
            try
            {
                // secondTemplateDirectory is always equal to or under targetDirectory
                if (Directory.Exists(targetDirectory))
                {
                    Directory.Delete(targetDirectory, true);
                }
                Directory.CreateDirectory(targetDirectory);
                Directory.CreateDirectory(secondTemplateDirectory);
                File.WriteAllText(firstTemplatePath, firstTemplateString);
                File.WriteAllText(secondTemplatePath, secondTemplateString);
                if (nestedTemplate != null)
                {
                    File.WriteAllText(
                        Path.Combine(targetDirectory, nestedTemplate),
                        ReadTemplate(nestedTemplate));
                }

                using var writer = SetupWriter(memStream, targetDirectory);
                var analyzer = TemplateAnalyzer.Create(false);

                var results = analyzer.AnalyzeTemplate(
                    template: firstTemplateString,
                    parameters: null,
                    templateFilePath: firstTemplateFileInfo.FullName);
                writer.WriteResults(results, (FileInfoBase)firstTemplateFileInfo);
                analyzerResults = results.ToList();

                results = analyzer.AnalyzeTemplate(
                    template: secondTemplateString,
                    parameters: null,
                    templateFilePath: secondTemplateFileInfo.FullName);
                writer.WriteResults(results, (FileInfoBase)secondTemplateFileInfo);
                analyzerResults.AddRange(results.ToList());
            }
            finally
            {
                Directory.Delete(targetDirectory, true);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(Encoding.UTF8.GetString(memStream.ToArray()));
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Rules.Count.Should().Be(10);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000022")).Should().Be(true);
            run.Tool.Driver.Rules.Any(r => r.Id.Equals("TA-000028")).Should().Be(true);
            run.OriginalUriBaseIds.Count.Should().Be(1);
            run.OriginalUriBaseIds["ROOTPATH"].Uri.Should().Be(new Uri(targetDirectory, UriKind.Absolute));

            var isBicep = (firstTemplateFileInfo.Extension == ".bicep");
            var expectedLinesForRun = new Dictionary<string, (string file, string uriBase, List<List<int>> lines)>
            {
                { "TA-000022", (
                    file: firstTemplate,
                    uriBase: SarifReportWriter.UriBaseIdString,
                    lines: new List<List<int>> {
                        isBicep ? new List<int> { 8 } : new List<int> { 20 }
                    })
                },
                { "TA-000028", (
                    file: isBicep ? nestedTemplate : expectedSecondTemplateFilePathInSarif,
                    uriBase: secondTemplateUsesRelativePath ? SarifReportWriter.UriBaseIdString : null,
                    lines: new List<List<int>> {
                        isBicep ? new List<int> { 14, 15, 16 } : new List<int> { 23, 24, 25 },
                        isBicep ? new List<int> { 31, 32, 33 } : new List<int> { 43, 44, 45 }
                    })
                },
                { "AZR-000164", (
                    file: firstTemplate,
                    uriBase: SarifReportWriter.UriBaseIdString,
                    lines: new List<List<int>> {
                        isBicep ? new List<int> { 7 } : new List<int> { 19 }
                    })
                },
                { "AZR-000165", (
                    file: firstTemplate,
                    uriBase: SarifReportWriter.UriBaseIdString,
                    lines: new List<List<int>> {
                        isBicep ? new List<int> { 7 } : new List<int> { 19 }
                    })
                },
                { "AZR-000186", (
                    file: expectedSecondTemplateFilePathInSarif,
                    uriBase: secondTemplateUsesRelativePath ? SarifReportWriter.UriBaseIdString : null,
                    lines: new List<List<int>> {
                        new List<int> { 1 }
                    })
                },
                { "AZR-000187", (
                    file: isBicep ? nestedTemplate : expectedSecondTemplateFilePathInSarif,
                    uriBase: secondTemplateUsesRelativePath ? SarifReportWriter.UriBaseIdString : null,
                    lines: new List<List<int>> {
                        isBicep ? new List<int> { 5 } : new List<int> { 14 },
                        isBicep ? new List<int> { 22 } : new List<int> { 34 }
                    })
                },
                { "AZR-000188", (
                    file: isBicep ? nestedTemplate : expectedSecondTemplateFilePathInSarif,
                    uriBase: secondTemplateUsesRelativePath ? SarifReportWriter.UriBaseIdString : null,
                    lines: new List<List<int>> {
                        isBicep ? new List<int> { 5 } : new List<int> { 14 },
                        isBicep ? new List<int> { 22 } : new List<int> { 34 }
                    })
                },
                { "AZR-000189", (
                    file: isBicep ? nestedTemplate : expectedSecondTemplateFilePathInSarif,
                    uriBase: secondTemplateUsesRelativePath ? SarifReportWriter.UriBaseIdString : null,
                    lines: new List<List<int>> {
                        isBicep ? new List<int> { 5 } : new List<int> { 14 },
                        isBicep ? new List<int> { 22 } : new List<int> { 34 }
                    })
                },
                { "AZR-000299", (
                    file: firstTemplate,
                    uriBase: SarifReportWriter.UriBaseIdString,
                    lines: new List<List<int>> {
                        new List<int> { 1 }
                    })
                },
                { "AZR-000300", (
                    file: firstTemplate,
                    uriBase: SarifReportWriter.UriBaseIdString,
                    lines: new List<List<int>> {
                        new List<int> { 1 }
                    })
                }
            };

            // Extra validation for extra results in parent bicep template
            if (nestedTemplate != null)
            {
                var extraResults = run.Results.Where(result =>
                    (result.RuleId == "AZR-000299" || result.RuleId == "AZR-000300") &&
                    result.Locations.Count == 1 &&
                    result.Locations[0].PhysicalLocation.Region.StartLine == 1 &&
                    result.Locations[0].PhysicalLocation.ArtifactLocation.Uri.OriginalString == secondTemplate);
                extraResults.Count().Should().Be(2);
                extraResults.ForEach(r => run.Results.Remove(r));
            }

            run.Results.Count.Should().Be(14);
            foreach (Result result in run.Results)
            {
                expectedLinesForRun.ContainsKey(result.RuleId).Should().BeTrue("Unexpected result found in SARIF");

                result.Locations.Count.Should().BeGreaterThan(0);

                // Find correct set of lines expected for result
                (var fileName, var uriBase, var linesForResult) = expectedLinesForRun[result.RuleId];
                var expectedLines = linesForResult.FirstOrDefault(l => l.Contains(result.Locations.First().PhysicalLocation.Region.StartLine));
                expectedLines.Should().NotBeNull("There shouldn't be a line number reported outside of the expected lines.");
                linesForResult.Remove(expectedLines);

                // Validate analysis target if result is from nested template
                if (fileName == nestedTemplate && result.RuleId != "AZR-000186") // special case for rule, not in referenced template regardless
                {
                    result.AnalysisTarget.Uri.OriginalString.Should().BeEquivalentTo(secondTemplate);
                }
                else
                {
                    result.AnalysisTarget.Should().BeNull();
                }

                // Verify lines reported equal the expected lines
                result.Locations.Count.Should().Be(expectedLines.Count);
                foreach (var location in result.Locations)
                {
                    location.PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo(fileName);
                    location.PhysicalLocation.ArtifactLocation.UriBaseId.Should().BeEquivalentTo(uriBase);

                    // Verify line is expected, and remove from the collection
                    var line = location.PhysicalLocation.Region.StartLine;
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

                result.Level.Should().Be(GetLevelFromSeverity(analyzerResults.First(r => result.RuleId == r.RuleId).Severity));
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
                .Returns(() => new MockFileStream(stream));
            return new SarifReportWriter(mockFileSystem.Object, targetPath: targetDirectory);
        }

        private static FailureLevel GetLevelFromSeverity(Types.Severity severity) =>
            severity switch
            {
                Types.Severity.Low => FailureLevel.Note,
                Types.Severity.Medium => FailureLevel.Warning,
                Types.Severity.High => FailureLevel.Error,
                _ => FailureLevel.Error,
            };
    }
}
