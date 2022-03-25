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

                Assert.AreEqual(expectedLineNumber, evaluation.Result.LineNumber);
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
                    Assert.AreEqual(expectedLineNumber, evaluationResult.Result.LineNumber);
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
        [ExpectedException(typeof(JsonRuleEngineException))]
        public void FilterRules_ConfigurationIsInvalid_ExceptionIsThrown()
        {
            var rule = @"[{
                ""id"": ""RuleId 0"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": 1,
                ""evaluation"": { }
            }]";
            // Act
            var jsonRuleEngine = JsonRuleEngine.Create(rule, t => null);
            jsonRuleEngine.FilterRules("falsePath");
        }

        [DataTestMethod]
        [DataRow("", 5, DisplayName = "Entire RuleSet; Empty configuration")]
        [DataRow(@"{
                ""inclusions"": {
                        ""severity"": [3]
                    }
            }", 3, new int[] { 3 }, DisplayName = "Only Severity 3")]
        [DataRow(@"{
                ""exclusions"": {
                        ""severity"": [3]
                    }
            }", 2, null, null, new int[] { 3 }, DisplayName = "Excludes Severity 3")]
        [DataRow(@"{
                ""inclusions"": {
                        ""ids"": [""RuleId0""]
                    }
            }", 1, null, new string[] { "RuleId0" }, DisplayName = "Only Id RuleId0")]
        [DataRow(@"{
                ""exclusions"": {
                        ""ids"": [""RuleId0""]
                    }
            }", 4, null, null, null, new string[] { "TA-000001" }, DisplayName = "All rules except Id RuleId0")]
        [DataRow(@"{
                ""inclusions"": {
                        ""severity"": [3],
                        ""ids"": [""RuleId0""]
                    }
            }", 4, new int[] { 3 }, new string[] { "RuleId0" }, DisplayName = "Only Severity 3 and  Id RuleId0")]
        [DataRow(@"{
                ""inclusions"": {
                        ""ids"": [""RuleId0""]
                    }, 
                ""severityOverrides"": {
                    ""RuleId0"": 3
                    }
                }", 1, new int[] { 3 }, new string[] { "RuleId0" }, null, null, DisplayName = "Only Id RuleId0, change Severity to 3")]
        [DataRow(@"{
                ""inclusions"": {
                        ""severity"": [3]
                    },
                ""exclusions"": {
                        ""severity"": [3]
                    }
            }", 3, new int[] { 3 }, DisplayName = "Only Severity 3 from inclusions object; Exclusions object is ignored")]
        public void FilterRules_ValidInputValues_ReturnCorrectFilteredRules(string configuration, int expectedRuleCount, 
            IEnumerable<Severity> includeSeverities = null, IEnumerable<string> includeIds = null, 
            IEnumerable<Severity> excludeSeverities = null, IEnumerable<string> excludeIds = null)
        {
            // Setup mock Rules
            var mockRules = @"[{
                ""id"": ""RuleId0"",
                ""description"": ""Rule description"",
                ""recommendation"": ""Recommendation"",
                ""helpUri"": ""Uri"",
                ""severity"": 1,
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
                ""severity"": 3,
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

            // Arrange
            var jsonRuleEngine = JsonRuleEngine.Create(mockRules, t => null);

            // Filter
            jsonRuleEngine.FilterRules(configuration);

            // Compare
            Assert.AreEqual(expectedRuleCount, jsonRuleEngine.RuleDefinitions.Count);

            if (includeSeverities != null || includeIds != null)
            {
                foreach (var r in jsonRuleEngine.RuleDefinitions)
                {
                    Assert.IsTrue((includeSeverities != null && includeSeverities.Contains(r.Severity)) ||
                        (includeIds != null && includeIds.Contains(r.Id)));
                }
            }
            if (excludeSeverities != null || excludeIds != null)
            {
                foreach (var r in jsonRuleEngine.RuleDefinitions)
                {
                    Assert.IsTrue((excludeSeverities != null && !excludeSeverities.Contains(r.Severity)) ||
                        (excludeIds != null && !excludeIds.Contains(r.Id)));
                }
            }
        }
    }
}