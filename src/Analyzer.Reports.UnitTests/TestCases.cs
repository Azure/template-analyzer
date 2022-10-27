// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    public class TestCases
    {
        public static string GetTestCaseName(MethodInfo _, object[] testData) => (string)testData[0];

        public static IReadOnlyCollection<object[]> UnitTestCases => new List<object[]>
        {
            new object[]
            {
                "Single evaluation with failed result",
                new []
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, LineNumber = 10 }
                    }
                }
            },
            new object[]
            {
                "Multiple evaluations with failed results, different rules",
                new []
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, LineNumber = 65 }
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, LineNumber = 120 }
                    }
                }
            },
            new object[]
            {
                "Multiple evaluations with failed results, duplicate rules",
                new []
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, LineNumber = 65 }
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, LineNumber = 65 }
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, LineNumber = 120 }
                    }
                }
            },
            new object[]
            {
                "Single evaluation with nested evaluations",
                new[]
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 9 }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 117 }
                            },
                        },
                    }
                }
            },
            new object[]
            {
                "Multiple nested evaluations",
                new[]
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 9 }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 23 }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help#0000002",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 130 }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 245 }
                                    }
                                }
                            },
                        },
                    }
                }
            },
            new object[]
            {
                "Multiple nested evaluations with duplicate rules",
                new[]
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 9 }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 23 }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help#0000002",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 130 }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 245 }
                                    }
                                }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help#0000002",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 130 }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 245 }
                                    }
                                }
                            },
                        },
                    }
                }
            },
            new object[]
            {
                "Multiple nested evaluations with mixed pass/fail results",
                new[]
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 9 }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 117 }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help#0000002",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 130 }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 618 }
                                    }
                                }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000003",
                        RuleName = "Rule000003",
                        RuleDescription = "Test rule 0000003",
                        Recommendation = "Recommendation 0000003",
                        HelpUri = "https://domain.com/help#0000003",
                        Passed = true,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 130 }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = true, LineNumber = 618 }
                                    }
                                }
                            },
                        },
                    }
                }
            },
            new object[]
            {
                "Multiple nested evaluations with mixed pass/fail results and repeated rules",
                new[]
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 9 }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 117 }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help#0000002",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 130 }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 618 }
                                    }
                                }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help#0000002",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 130 }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 618 }
                                    }
                                }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000003",
                        RuleName = "Rule000003",
                        RuleDescription = "Test rule 0000003",
                        Recommendation = "Recommendation 0000003",
                        HelpUri = "https://domain.com/help#0000003",
                        Passed = true,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, LineNumber = 130 }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = true, LineNumber = 618 }
                                    }
                                }
                            },
                        },
                    }
                }
            },
            new object[]
            {
                "Multiple nested evaluations with all passed results",
                new[]
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = true,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = true, LineNumber = 9 }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, LineNumber = 117 }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help#0000002",
                        Passed = true,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, LineNumber = 25 }
                            },
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = new []
                                {
                                    new MockEvaluation
                                    {
                                        Passed = true,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = true, LineNumber = 130 }
                                    }
                                }
                            },
                        },
                    }
                }
            },
            new object[]
            {
                "Evaluations with same line flagged multiple times",
                new[]
                {
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleName = "Rule000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 9 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 9 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 15 }
                            }
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleName = "Rule000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 45 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 45 }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, LineNumber = 50 }
                            }
                        }
                    }
                }
            }
        }.AsReadOnly();
    }
}
