// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    public class TestCases
    {
        public static string TestTemplateFilePath = @"C:\Users\User\Azure\AppServices.json";

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
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 10) }
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
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 65) }
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 120) }
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
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 65) }
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 65) }
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
                        RuleDescription = "Test rule 0000002",
                        Recommendation = "Recommendation 0000002",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 120) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 9) }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 117) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 9) }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 23) }
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
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 245) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 9) }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 23) }
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
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 245) }
                                    }
                                }
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
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 245) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 9) }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 117) }
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
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 618) }
                                    }
                                }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000003",
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
                                Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 618) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 9) }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 117) }
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
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 618) }
                                    }
                                }
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
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 618) }
                                    }
                                }
                            },
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000003",
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
                                Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
                                    },
                                    new MockEvaluation
                                    {
                                        Passed = false,
                                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                                        Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 618) }
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
                                        Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 9) }
                                    }
                                }
                            },
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 117) }
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
                        Evaluations = new []
                        {
                            new MockEvaluation
                            {
                                Passed = true,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 25) }
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
                                        Result = new MockResult { Passed = true, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 130) }
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
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 9) }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 9) }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 15) }
                            }
                        },
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000002",
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
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 45) }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 45) }
                            },
                            new MockEvaluation
                            {
                                Passed = false,
                                Evaluations = Enumerable.Empty<MockEvaluation>(),
                                Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 50) }
                            }
                        }
                    }
                }
            },
            new object[]
            {
                "Evaluations in multiple files",
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
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(TestTemplateFilePath, 10) }
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(@"C:\Users\User\Azure\RedisCache.json", 15,
                            new Types.SourceLocation(TestTemplateFilePath, 20)) }
                    },
                    new MockEvaluation
                    {
                        RuleId = "TEST-000001",
                        RuleDescription = "Test rule 0000001",
                        Recommendation = "Recommendation 0000001",
                        HelpUri = "https://domain.com/help",
                        Passed = false,
                        Evaluations = Enumerable.Empty<MockEvaluation>(),
                        Result = new MockResult { Passed = false, SourceLocation = new Types.SourceLocation(@"C:\Users\User\Azure\SqlServer.json", 15,
                            new Types.SourceLocation(TestTemplateFilePath, 30)) }
                    }
                }
            },
        }.AsReadOnly();
    }
}
