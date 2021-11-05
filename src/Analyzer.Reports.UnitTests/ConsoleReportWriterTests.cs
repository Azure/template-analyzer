// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    [TestClass]
    public class ConsoleReportWriterTests
    {
        [TestMethod]
        public void ConsoleReportWriter_SingleEvaluationSingleFailedResult()
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(testcases, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, testcases, templateFilePath);
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(testcases, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, testcases, templateFilePath);
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(testcases, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, testcases, templateFilePath);
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(testcases, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, testcases, templateFilePath);
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(testcases, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, testcases, templateFilePath);
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(testcases, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, testcases, templateFilePath);
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(testcases, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, testcases, templateFilePath);
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
            var output = new StringWriter();
            Console.SetOut(output);
            using (var writer = new ConsoleReportWriter())
            {
                writer.WriteResults(testcases, (FileInfoBase)templateFilePath);
            }

            // assert
            AssertConsoleLog(output, testcases, templateFilePath);
        }

        private void AssertConsoleLog(StringWriter output, IEnumerable<Types.IEvaluation> testcases, FileInfo templateFilePath)
        {
            string outputString = output.ToString();
            StringBuilder expected = new StringBuilder();
            expected.Append($"{Environment.NewLine}{Environment.NewLine}File: {templateFilePath}{Environment.NewLine}");

            foreach (var evaluation in testcases.Where(e => !e.Passed))
            {
                expected.Append($"{ConsoleReportWriter.IndentedNewLine}{(!string.IsNullOrEmpty(evaluation.RuleId) ? $"{evaluation.RuleId}: " : string.Empty)}{evaluation.RuleDescription}");
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
            StringBuilder resultString = new StringBuilder();
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
