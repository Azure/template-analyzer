// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    public class TestCases
    {
        public static IEnumerable<IEnumerable<MockEvaluation>> UnitTestCases = new[]
        {
            // Single evaluation with single failed result
            new []
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
            },
            // Single evaluation with multiple failed results
            new[]
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
            },
            // Multiple evaluations with multiple failed results
            new []
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
            },
            // Single nested evaluation
            new[]
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
            },
            // Multiple nested evaluations
            new[]
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
            },
            // Multiple nested evaluations with mixed pass/fail results
            new[]
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
            },
            // Multiple nested evaluations with all passed results
            new[]
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
            },
            // Evaluation without results
            new[]
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
            }
        };
    }
}
