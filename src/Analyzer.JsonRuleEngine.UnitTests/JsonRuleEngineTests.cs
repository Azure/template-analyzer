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
            var mockLineResolver = new Mock<ILineNumberResolver>();
            mockLineResolver.Setup(r =>
                r.ResolveLineNumber(
                    It.IsAny<string>()))
                .Returns(expectedLineNumber);

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

                foreach (var result in evaluation.Results)
                {
                    Assert.AreEqual(expectedLineNumber, result.LineNumber);
                }
            }
        }

        [DataTestMethod]
        [DataRow(@"[{
                ""id"": ""RuleId 0"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": ""someInt"",
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
            var mockLineResolver = new Mock<ILineNumberResolver>();
            mockLineResolver.Setup(r =>
                r.ResolveLineNumber(
                    It.IsAny<string>()))
                .Returns(expectedLineNumber);

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

            Assert.AreEqual(0, evaluation.Results.Count());

            AssertEvaluationsAndResultsAreAsExpected(evaluation, expectedLineNumber);
        }

        private void AssertEvaluationsAndResultsAreAsExpected(IEvaluation evaluation, int expectedLineNumber)
        {
            foreach (var evaluationResult in evaluation.Evaluations)
            {
                if ((evaluationResult as JsonRuleEvaluation).Expression is LeafExpression)
                {
                    foreach (var result in evaluationResult.Results)
                    {
                        Assert.AreEqual(expectedLineNumber, result.LineNumber);
                    }
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
    }
}