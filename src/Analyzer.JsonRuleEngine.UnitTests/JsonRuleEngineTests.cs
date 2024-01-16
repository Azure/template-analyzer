// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class JsonRuleEngineTests
    {
        [DataTestMethod]
        [DataRow(new string[] {
                @"{
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""path"": ""properties.somePath"",
                    ""hasValue"": true
                }"
            },
            1, DisplayName = "1 Rule That Passes")]
        [DataRow(
            new string[] {
                @"{
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""path"": ""properties.someOtherPath"",
                    ""hasValue"": true
                }",
                @"{
                    ""path"": ""$schema"",
                    ""hasValue"": true
                }"
            },
            1, DisplayName = "2 Rules, 1 Passes, 1 Fails")]
        public void AnalyzeTemplate_ValidLeafExpression_ReturnsExpectedEvaluations(string[] ruleEvaluationDefinitions, int numberOfExpectedPassedResults)
        {
            // Arrange
            var template =
                JObject.Parse(
                @"{
                    ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                    ""resources"": [
                        {
                            ""type"": ""Microsoft.ResourceProvider/resource0"",
                            ""properties"": {
                                ""somePath"": ""someValue""
                            }
                        }
                    ]
                }");

            string expectedFileId = "MyTemplate";
            int expectedLineNumber = 5;

            var rules = CreateRulesFromEvaluationDefinitions(ruleEvaluationDefinitions);

            TemplateContext templateContext = new TemplateContext { 
                OriginalTemplate = template, 
                ExpandedTemplate = template, 
                IsMainTemplate = true,
                TemplateIdentifier = expectedFileId
            };

            // Setup mock line number resolver
            var mockLineResolver = new Mock<ISourceLocationResolver>();
            mockLineResolver.Setup(r =>
                r.ResolveSourceLocation(
                    It.IsAny<string>()))
                .Returns(new SourceLocation(default, expectedLineNumber));

            var ruleEngine = JsonRuleEngine.Create(rules, t => {
                    // Verify the test context was passed
                    if (t == templateContext)
                    {
                        return mockLineResolver.Object;
                    }
                    Assert.Fail("Expected template context was not passed to LineNumberResolver.");
                    return null;
                });

            // Act
            var evaluationResults = ruleEngine.AnalyzeTemplate(templateContext).ToList();

            // Assert
            Assert.AreEqual(ruleEvaluationDefinitions.Length, evaluationResults.Count);
            Assert.AreEqual(numberOfExpectedPassedResults, evaluationResults.Count(result => result.Passed));
            for (int i = 0; i < ruleEvaluationDefinitions.Length; i++)
            {
                var evaluation = evaluationResults[i];
                Assert.AreEqual($"RuleId {i}", evaluation.RuleId);
                Assert.AreEqual(expectedFileId, evaluation.FileIdentifier);

                Assert.AreEqual(expectedLineNumber, evaluation.Result.SourceLocation.LineNumber);
            }
        }

        [DataTestMethod]
        [DataRow(@"[{
                ""id"": ""RuleId 0"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""evaluation"": {
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""allOf"": [
                        {
                            ""path"": ""properties.somePath"",
                            ""hasValue"": true
                        },
                        {
                            ""path"": ""properties.somePath"",
                            ""equals"": ""someValue""
                        }
                    ]
                }
            }]", DisplayName = "Single allOf expression")]
        [DataRow(@"[{
                ""id"": ""RuleId 0"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""evaluation"": {
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""allOf"": [
                        {
                            ""allOf"": [
                                {
                                    ""path"": ""properties.somePath"",
                                    ""hasValue"": true
                                },
                                {
                                    ""path"": ""properties.somePath"",
                                    ""exists"": true
                                }
                            ]
                            
                        },
                        {
                            ""path"": ""properties.somePath"",
                            ""equals"": ""someValue""
                        }
                    ]
                }
            }]", DisplayName = "Nested allOf expression")]
        [DataRow(@"[{
                ""id"": ""RuleId 0"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""evaluation"": {
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""anyOf"": [
                        {
                            ""path"": ""properties.somePath"",
                            ""equals"": ""someValue""
                        },
                        {
                            ""path"": ""properties.someOtherPath"",
                            ""equals"": ""someOtherValue2""
                        }
                    ]
                }
            }]", DisplayName = "Single anyOf expression")]
        public void AnalyzeTemplate_ValidStructuredExpression_ReturnsExpectedEvaluations(string rules)
        {
            // Arrange
            var template =
                JObject.Parse(
                @"{
                    ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                    ""resources"": [
                        {
                            ""type"": ""Microsoft.ResourceProvider/resource0"",
                            ""properties"": {
                                ""somePath"": ""someValue"",
                                ""someOtherPath"": ""someOtherValue""
                            }
                        }
                    ]
                }");

            string expectedFileId = "MyTemplate";
            int expectedLineNumber = 5;

            TemplateContext templateContext = new TemplateContext
            {
                OriginalTemplate = template,
                ExpandedTemplate = template,
                IsMainTemplate = true,
                TemplateIdentifier = expectedFileId
            };

            // Setup mock line number resolver
            var mockLineResolver = new Mock<ISourceLocationResolver>();
            mockLineResolver.Setup(r =>
                r.ResolveSourceLocation(
                    It.IsAny<string>()))
                .Returns(new SourceLocation(default, expectedLineNumber));

            var ruleEngine = JsonRuleEngine.Create(rules, t =>
            {
                // Verify the test context was passed
                if (t == templateContext)
                {
                    return mockLineResolver.Object;
                }
                Assert.Fail("Expected template context was not passed to LineNumberResolver.");
                return null;
            });

            var evaluationResults = ruleEngine.AnalyzeTemplate(templateContext).ToList();

            Assert.AreEqual(1, evaluationResults.Count);
            Assert.AreEqual(1, evaluationResults.Count(evaluation => evaluation.Passed));

            var evaluation = evaluationResults[0];
            Assert.AreEqual($"RuleId 0", evaluation.RuleId);
            Assert.AreEqual(expectedFileId, evaluation.FileIdentifier);
            Assert.AreEqual(Severity.Medium, evaluation.Severity); //Default value

            Assert.IsNull(evaluation.Result);

            AssertEvaluationsAndResultsAreAsExpected(evaluation, expectedLineNumber);
        }

        private void AssertEvaluationsAndResultsAreAsExpected(IEvaluation evaluation, int expectedLineNumber)
        {
            foreach (var evaluationResult in evaluation.Evaluations)
            {
                if ((evaluationResult as JsonRuleEvaluation).Expression is LeafExpression)
                {
                    Assert.AreEqual(expectedLineNumber, evaluationResult.Result.SourceLocation.LineNumber);
                }
                else
                {
                    AssertEvaluationsAndResultsAreAsExpected(evaluationResult, expectedLineNumber);
                }
            }
        }

        [DataTestMethod]
        [DataRow(@"[{
                ""id"": ""Invalid Rule"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""evaluation"": {
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    {
                        ""path"": ""properties.somePath"",
                        ""regex"": ""[""
                    }
                }
            }]", DisplayName = "Invalid JSON")]
        [DataRow(@"[{
                ""id"": ""Invalid Rule"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""evaluation"": {
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""path"": ""properties.somePath"",
                    ""regex"": ""[""
                }
            }]", DisplayName = "Invalid regex pattern")]
        [ExpectedException(typeof(JsonRuleEngineException))]
        public void Create_InvalidRules_ExceptionIsThrown(string invalidRule)
        {
            // Act
            JsonRuleEngine.Create(invalidRule, t => null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Create_NullRules_ExceptionIsThrown()
        {
            // Act
            JsonRuleEngine.Create(null, t => null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Create_EmptyRules_ExceptionIsThrown()
        {
            // Act
            JsonRuleEngine.Create("", t => null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Create_WhitespaceRules_ExceptionIsThrown()
        {
            // Act
            JsonRuleEngine.Create("  \t", t => null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Create_NullLineNumberResolver_ExceptionIsThrown()
        {
            // Act
            JsonRuleEngine.Create(CreateRulesFromEvaluationDefinitions(
                new[] {
                    @"{
                        ""path"": ""$schema"",
                        ""hasValue"": true
                    }"}),
                null);
        }

        private string CreateRulesFromEvaluationDefinitions(string[] ruleEvaluationDefinitions)
        {
            string ruleSkeleton = @"{{
                ""id"": ""RuleId {0}"",
                ""description"": ""Rule description {0}"",
                ""recommendation"": ""Recommendation {0}"",
                ""helpUri"": ""Uri {0}"",
                ""evaluation"": {1}
            }}";

            List<string> rules = new List<string>();
            for (int i = 0; i < ruleEvaluationDefinitions.Length; i++)
            {
                var evaluation = ruleEvaluationDefinitions[i];
                rules.Add(string.Format(ruleSkeleton, i, evaluation));
            }

            return $"[{string.Join(",", rules)}]";
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FilterRules_ConfigurationIsNull_ExceptionIsThrown()
        {
            var rule = @"[{
                ""id"": ""RuleId 0"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": 1,
                ""evaluation"": {
                    ""path"": ""$schema"",
                    ""hasValue"": true
                }
            }]";
            // Act
            var jsonRuleEngine = JsonRuleEngine.Create(rule, t => null);
            jsonRuleEngine.FilterRules(null);
        }

        [DataTestMethod]
        [DataRow("{}", "RuleId0", "RuleId1", "RuleId2", "RuleId3", "RuleId4", DisplayName = "Entire RuleSet; Empty configuration")]
        [DataRow(@"{
                ""inclusions"": {
                        ""severity"": [3]
                    }
            }", "RuleId2", "RuleId3", "RuleId4", DisplayName = "Include Severity 3")]
        [DataRow(@"{
                ""exclusions"": {
                        ""severity"": [""Low""]
                    }
            }", "RuleId0", "RuleId1", DisplayName = "Exclude Severity 3")]
        [DataRow(@"{
                ""inclusions"": {
                        ""ids"": [""RuleId0""]
                    }
            }", "RuleId0", DisplayName = "Only Id RuleId0")]
        [DataRow(@"{
                ""inclusions"": {},
                ""exclusions"": {
                        ""ids"": [""RuleId3""]
                    }
            }", "RuleId0", "RuleId1", "RuleId2", "RuleId4", DisplayName = "Empty inclusions object, exclude RuleId3")]
        [DataRow(@"{
                ""exclusions"": {
                        ""ids"": [""RuleId3""]
                    }
            }", "RuleId0", "RuleId1", "RuleId2", "RuleId4", DisplayName = "Exclude RuleId3")]
        [DataRow(@"{
                ""inclusions"": {
                        ""severity"": [""Low""],
                        ""ids"": [""RuleId0""]
                    }
            }", "RuleId0", "RuleId2", "RuleId3", "RuleId4", DisplayName = "Include Severity 3 and Id RuleId0")]
        [DataRow(@"{
                ""exclusions"": {
                        ""severity"": [2],
                        ""ids"": [""RuleId0""]
                    }
            }", "RuleId2", "RuleId3", "RuleId4", DisplayName = "Exclude Severity 2 and RuleId0")]
        [DataRow(@"{
                ""inclusions"": {
                        ""ids"": [""RuleId0""]
                    }, 
                ""severityOverrides"": {
                    ""RuleId0"": 2
                    }
                }", "RuleId0:2", DisplayName = "Include RuleId0, change Severity to 2")]
        [DataRow(@"{
                ""severityOverrides"": {
                    ""RuleId0"": ""Low"",
                    ""RuleId2"": 1,
                    ""RuleId4"": ""Medium""
                    }
                }", "RuleId0:3", "RuleId1", "RuleId2:1", "RuleId3", "RuleId4:2", DisplayName = "All rules included, multiple severities changed")]
        [DataRow(@"{
                ""inclusions"": {
                        ""severity"": [3]
                    },
                ""exclusions"": {
                        ""ids"": [""RuleId2""]
                    }
            }", "RuleId2", "RuleId3", "RuleId4", DisplayName = "Include Severity 3; Exclusions object is ignored")]
        [DataRow(@"{
                ""exclusions"": {},
                ""inclusions"": {
                        ""ids"": [""RuleId3""]
                    }
            }", "RuleId3", DisplayName = "Empty exclusions object listed first, include RuleId3; Exclusions object is ignored")]
        [DataRow(@"{
                ""exclusions"": {
                        ""ids"": [""RuleId3""]
                    },
                ""inclusions"": {
                        ""ids"": [""RuleId3""]
                    }
            }", "RuleId3", DisplayName = "Exclusions object listed first, include RuleId3; Exclusions object is ignored")]
        [DataRow(@"{
                ""inclusions"": {
                        ""severity"": [1]
                    }, 
                ""severityOverrides"": {
                    ""RuleId0"": 1
                    }
            }", "RuleId0", DisplayName = "Include Severity 1; override severity to same value as present produces no error")]
        [DataRow(@"{
                ""inclusions"": {
                        ""severity"": [1]
                    }, 
                ""severityOverrides"": {
                    ""RuleId1"": 1
                    }
            }", "RuleId0", DisplayName = "Include Severity 1; override severity on a not-include rule, rule still not included")]
        public void FilterRules_ValidInputValues_ReturnCorrectFilteredRules(string configuration, params string[] expectedRules)
        {
            // Setup mock Rules
            var mockRules = @"[{
                ""id"": ""RuleId0"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": ""High"",
                ""evaluation"": { 
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""path"": ""properties.somePath"",
                    ""hasValue"": true
                }
            },
            {
                ""id"": ""RuleId1"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": 2,
                ""evaluation"": { 
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""path"": ""properties.somePath"",
                    ""hasValue"": true
                }
            },
            {
                ""id"": ""RuleId2"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": 3,
                ""evaluation"": { 
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""path"": ""properties.somePath"",
                    ""hasValue"": true
                }
            },
            {
                ""id"": ""RuleId3"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": ""Low"",
                ""evaluation"": { 
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""path"": ""properties.somePath"",
                    ""hasValue"": true
                }
            },
            {
                ""id"": ""RuleId4"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": 3,
                ""evaluation"": { 
                    ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                    ""path"": ""properties.somePath"",
                    ""hasValue"": true
                }
            }]";

            var parsedRules = JArray.Parse(mockRules);

            // Arrange
            var jsonRuleEngine = JsonRuleEngine.Create(mockRules, t => null);

            // Filter
            jsonRuleEngine.FilterRules(JsonConvert.DeserializeObject<ConfigurationDefinition>(configuration));

            // Compare
            Assert.AreEqual(expectedRules.Length, jsonRuleEngine.RuleDefinitions.Count);

            foreach (var rulePair in expectedRules)
            {
                var ruleParts = rulePair.Split(':');
                Assert.IsTrue(ruleParts.Length < 3, "Invalid test expectation - must be just the Rule Id or of the form \"<rule id>:<severity>\"");
                string ruleName = ruleParts[0], ruleSeverity = ruleParts.Length > 1 ? ruleParts[1] : string.Empty;

                var includedRule = jsonRuleEngine.RuleDefinitions.SingleOrDefault(r => r.Id == ruleName);
                Assert.IsNotNull(includedRule);

                Severity expectedSeverity = Enum.Parse<Severity>(
                    ruleSeverity != string.Empty
                        ? ruleSeverity
                        : parsedRules.Single(r => r["id"].Value<string>() == ruleName)["severity"].Value<string>()
                    );

                Assert.AreEqual(expectedSeverity, includedRule.Severity);
            }
        }
    }
}