using FluentAssertions;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    [TestClass]
    public class SarifReportWriterTests
    {
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

        private void AssertSarifLog(SarifLog sarifLog, IEnumerable<Types.IEvaluation> testcases, FileInfo templateFilePath)
        {
            sarifLog.Should().NotBeNull();

            Run run = sarifLog.Runs.First();
            run.Tool.Driver.Name.Should().BeEquivalentTo(SarifReportWriter.ToolName);
            run.Tool.Driver.FullName.Should().BeEquivalentTo(SarifReportWriter.ToolFullName);
            run.Tool.Driver.Version.Should().BeEquivalentTo(SarifReportWriter.ToolVersion);
            run.Tool.Driver.Organization.Should().BeEquivalentTo(SarifReportWriter.Organization);
            run.Tool.Driver.InformationUri.OriginalString.Should().BeEquivalentTo(SarifReportWriter.InformationUri);

            IList<ReportingDescriptor> rules = run.Tool.Driver.Rules;
            int ruleCount = testcases.Count(t => !t.Passed);
            if (ruleCount == 0)
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
                    rule.FullDescription.Text.Should().BeEquivalentTo(testcase.RuleDescription);
                    rule.Help.Text.Should().BeEquivalentTo(testcase.Recommendation);
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
                    results[i].Message.Text.Should().BeEquivalentTo(testcase.RuleDescription);
                    results[i].Level = res.Passed ? FailureLevel.Note : FailureLevel.Error;
                    results[i].Locations.First().PhysicalLocation.ArtifactLocation.Uri.OriginalString.Should().BeEquivalentTo("/" + templateFilePath.Name);
                    results[i].Locations.First().PhysicalLocation.Region.StartLine.Should().Be(res.LineNumber);
                    i++;
                }
            }
        }

        [TestMethod]
        public void SarifReportWriter_SingleEvaluationSingleFailedResult()
        {
            // arrange
            var testcases = new[]
            {
                new MockEvaluation
                {
                    RuleId = "TEST-000001",
                    RuleDescription = "Test rule 0000001",
                    Recommendation = "Recommendation 0000001",
                    HelpUri = "https://domain.com/help",
                    Passed = false,
                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                    Results = new[]
                    {
                        new MockResult { Passed = false, LineNumber = 10 }
                    }
                }
            };

            // act
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults((FileInfoBase)templateFilePath, testcases);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, testcases, templateFilePath);
        }

        [TestMethod]
        public void SarifReportWriter_SingleEvaluationMultipleFailedResult()
        {
            // arrange
            var testcases = new[]
            {
                new MockEvaluation
                {
                    RuleId = "TEST-000001",
                    RuleDescription = "Test rule 0000001",
                    Recommendation = "Recommendation 0000001",
                    HelpUri = "https://domain.com/help",
                    Passed = false,
                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                    Results = new[]
                    {
                        new MockResult { Passed = false, LineNumber = 10 },
                        new MockResult { Passed = false, LineNumber = 22 },
                        new MockResult { Passed = false, LineNumber = 65 },
                    }
                }
            };

            // act
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults((FileInfoBase)templateFilePath, testcases);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, testcases, templateFilePath);
        }

        [TestMethod]
        public void SarifReportWriter_MultiEvaluationsMultipleFailedResult()
        {
            // arrange
            var testcases = new[]
            {
                new MockEvaluation
                {
                    RuleId = "TEST-000001",
                    RuleDescription = "Test rule 0000001",
                    Recommendation = "Recommendation 0000001",
                    HelpUri = "https://domain.com/help",
                    Passed = false,
                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                    Results = new[]
                    {
                        new MockResult { Passed = false, LineNumber = 10 },
                        new MockResult { Passed = false, LineNumber = 22 },
                        new MockResult { Passed = false, LineNumber = 65 },
                    }
                },
                new MockEvaluation
                {
                    RuleId = "TEST-000002",
                    RuleDescription = "Test rule 0000002",
                    Recommendation = "Recommendation 0000002",
                    HelpUri = "https://domain.com/help",
                    Passed = false,
                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                    Results = new[]
                    {
                        new MockResult { Passed = false, LineNumber = 120 },
                        new MockResult { Passed = false, LineNumber = 632 },
                    }
                }
            };

            // act
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults((FileInfoBase)templateFilePath, testcases);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, testcases, templateFilePath);
        }

        [TestMethod]
        public void SarifReportWriter_SingleNestedEvaluations()
        {
            // arrange
            var testcases = new[]
            {
                new MockEvaluation
                {
                    RuleId = "TEST-000001",
                    RuleDescription = "Test rule 0000001",
                    Recommendation = "Recommendation 0000001",
                    HelpUri = "https://domain.com/help",
                    Passed = false,
                    Results = Enumerable.Empty<MockResult>(),
                    Evaluations = new []
                    {
                        new MockEvaluation
                        {
                            Results = Enumerable.Empty<MockResult>(),
                            Passed = false,
                            Evaluations = new []
                            {
                                new MockEvaluation
                                {
                                    Passed = false,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = false, LineNumber = 9 },
                                    }
                                }
                            }
                        },
                        new MockEvaluation
                        {
                            Passed = false,
                            Evaluations = Enumerable.Empty<MockEvaluation>(),
                            Results = new []
                            {
                                new MockResult { Passed = false, LineNumber = 23 },
                                new MockResult { Passed = false, LineNumber = 117 },
                            },
                        },
                    },
                }
            };

            // act
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults((FileInfoBase)templateFilePath, testcases);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, testcases, templateFilePath);
        }

        [TestMethod]
        public void SarifReportWriter_MultipleNestedEvaluations()
        {
            // arrange
            var testcases = new[]
            {
                new MockEvaluation
                {
                    RuleId = "TEST-000001",
                    RuleDescription = "Test rule 0000001",
                    Recommendation = "Recommendation 0000001",
                    HelpUri = "https://domain.com/help",
                    Passed = false,
                    Results = Enumerable.Empty<MockResult>(),
                    Evaluations = new []
                    {
                        new MockEvaluation
                        {
                            Results = Enumerable.Empty<MockResult>(),
                            Passed = false,
                            Evaluations = new []
                            {
                                new MockEvaluation
                                {
                                    Passed = false,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = false, LineNumber = 9 },
                                    }
                                }
                            }
                        },
                        new MockEvaluation
                        {
                            Passed = false,
                            Evaluations = Enumerable.Empty<MockEvaluation>(),
                            Results = new []
                            {
                                new MockResult { Passed = false, LineNumber = 23 },
                                new MockResult { Passed = false, LineNumber = 117 },
                            },
                        },
                    },
                },
                new MockEvaluation
                {
                    RuleId = "TEST-000002",
                    RuleDescription = "Test rule 0000002",
                    Recommendation = "Recommendation 0000002",
                    HelpUri = "https://domain.com/help#0000002",
                    Passed = false,
                    Results = Enumerable.Empty<MockResult>(),
                    Evaluations = new []
                    {
                        new MockEvaluation
                        {
                            Passed = false,
                            Evaluations = Enumerable.Empty<MockEvaluation>(),
                            Results = new []
                            {
                                new MockResult { Passed = false, LineNumber = 25 },
                                new MockResult { Passed = false, LineNumber = 72 },
                            },
                        },
                        new MockEvaluation
                        {
                            Results = Enumerable.Empty<MockResult>(),
                            Passed = false,
                            Evaluations = new []
                            {
                                new MockEvaluation
                                {
                                    Passed = false,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = false, LineNumber = 130 },
                                        new MockResult { Passed = false, LineNumber = 199 },
                                    }
                                },
                                new MockEvaluation
                                {
                                    Passed = false,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = false, LineNumber = 245 },
                                        new MockResult { Passed = false, LineNumber = 618 },
                                    }
                                }
                            }
                        },
                    },
                }
            };

            // act
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults((FileInfoBase)templateFilePath, testcases);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, testcases, templateFilePath);
        }

        [TestMethod]
        public void SarifReportWriter_MultipleNestedEvaluations_Mixed()
        {
            // mixed both passed/not passed evaluations/results
            // arrange
            var testcases = new[]
            {
                new MockEvaluation
                {
                    RuleId = "TEST-000001",
                    RuleDescription = "Test rule 0000001",
                    Recommendation = "Recommendation 0000001",
                    HelpUri = "https://domain.com/help",
                    Passed = false,
                    Results = Enumerable.Empty<MockResult>(),
                    Evaluations = new []
                    {
                        new MockEvaluation
                        {
                            Results = Enumerable.Empty<MockResult>(),
                            Passed = false,
                            Evaluations = new []
                            {
                                new MockEvaluation
                                {
                                    Passed = true,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = false, LineNumber = 9 },
                                    }
                                }
                            }
                        },
                        new MockEvaluation
                        {
                            Passed = false,
                            Evaluations = Enumerable.Empty<MockEvaluation>(),
                            Results = new []
                            {
                                new MockResult { Passed = false, LineNumber = 23 },
                                new MockResult { Passed = false, LineNumber = 117 },
                            },
                        },
                    },
                },
                new MockEvaluation
                {
                    RuleId = "TEST-000002",
                    RuleDescription = "Test rule 0000002",
                    Recommendation = "Recommendation 0000002",
                    HelpUri = "https://domain.com/help#0000002",
                    Passed = false,
                    Results = Enumerable.Empty<MockResult>(),
                    Evaluations = new []
                    {
                        new MockEvaluation
                        {
                            Passed = true,
                            Evaluations = Enumerable.Empty<MockEvaluation>(),
                            Results = new []
                            {
                                new MockResult { Passed = true, LineNumber = 25 },
                                new MockResult { Passed = true, LineNumber = 72 },
                            },
                        },
                        new MockEvaluation
                        {
                            Results = Enumerable.Empty<MockResult>(),
                            Passed = false,
                            Evaluations = new []
                            {
                                new MockEvaluation
                                {
                                    Passed = true,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = false, LineNumber = 130 },
                                        new MockResult { Passed = false, LineNumber = 199 },
                                    }
                                },
                                new MockEvaluation
                                {
                                    Passed = false,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = false, LineNumber = 245 },
                                        new MockResult { Passed = false, LineNumber = 618 },
                                    }
                                }
                            }
                        },
                    },
                }
            };

            // act
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults((FileInfoBase)templateFilePath, testcases);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, testcases, templateFilePath);
        }

        [TestMethod]
        public void SarifReportWriter_MultipleNestedEvaluations_AllPassed()
        {
            // mixed both passed/not passed evaluations/results
            // arrange
            var testcases = new[]
            {
                new MockEvaluation
                {
                    RuleId = "TEST-000001",
                    RuleDescription = "Test rule 0000001",
                    Recommendation = "Recommendation 0000001",
                    HelpUri = "https://domain.com/help",
                    Passed = true,
                    Results = Enumerable.Empty<MockResult>(),
                    Evaluations = new []
                    {
                        new MockEvaluation
                        {
                            Results = Enumerable.Empty<MockResult>(),
                            Passed = true,
                            Evaluations = new []
                            {
                                new MockEvaluation
                                {
                                    Passed = true,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = true, LineNumber = 9 },
                                    }
                                }
                            }
                        },
                        new MockEvaluation
                        {
                            Passed = true,
                            Evaluations = Enumerable.Empty<MockEvaluation>(),
                            Results = new []
                            {
                                new MockResult { Passed = true, LineNumber = 23 },
                                new MockResult { Passed = true, LineNumber = 117 },
                            },
                        },
                    },
                },
                new MockEvaluation
                {
                    RuleId = "TEST-000002",
                    RuleDescription = "Test rule 0000002",
                    Recommendation = "Recommendation 0000002",
                    HelpUri = "https://domain.com/help#0000002",
                    Passed = true,
                    Results = Enumerable.Empty<MockResult>(),
                    Evaluations = new []
                    {
                        new MockEvaluation
                        {
                            Passed = true,
                            Evaluations = Enumerable.Empty<MockEvaluation>(),
                            Results = new []
                            {
                                new MockResult { Passed = true, LineNumber = 25 },
                                new MockResult { Passed = true, LineNumber = 72 },
                            },
                        },
                        new MockEvaluation
                        {
                            Results = Enumerable.Empty<MockResult>(),
                            Passed = true,
                            Evaluations = new []
                            {
                                new MockEvaluation
                                {
                                    Passed = true,
                                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                                    Results = new []
                                    {
                                        new MockResult { Passed = true, LineNumber = 130 },
                                        new MockResult { Passed = true, LineNumber = 199 },
                                    }
                                }
                            }
                        },
                    },
                }
            };

            // act
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults((FileInfoBase)templateFilePath, testcases);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, testcases, templateFilePath);
        }

        [TestMethod]
        public void SarifReportWriter_EvaluationWithoutResults()
        {
            // arrange
            var testcases = new[]
            {
                new MockEvaluation
                {
                    RuleId = "TEST-000001",
                    RuleDescription = "Test rule 0000001",
                    Recommendation = "Recommendation 0000001",
                    HelpUri = "https://domain.com/help",
                    Passed = false,
                    Evaluations = Enumerable.Empty<MockEvaluation>(),
                    Results = Enumerable.Empty<MockResult>(),
                }
            };

            // act
            var templateFilePath = new FileInfo(@"C:\Users\User\Azure\AppServices.json");
            var memStream = new MemoryStream();
            using (var writer = SetupWriter(memStream))
            {
                writer.WriteResults((FileInfoBase)templateFilePath, testcases);
            }

            // assert
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(ASCIIEncoding.UTF8.GetString(memStream.ToArray()));
            AssertSarifLog(sarifLog, testcases, templateFilePath);
        }
    }
}
