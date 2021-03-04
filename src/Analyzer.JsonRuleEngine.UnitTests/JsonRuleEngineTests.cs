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
        public void EvaluateRules_ValidLeafExpression_ReturnsExpectedEvaluations(string[] ruleEvaluationDefinitions, int numberOfExpectedPassedResults)
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
            var mockLineResolver = new Mock<IJsonLineNumberResolver>();
            mockLineResolver.Setup(r =>
                r.ResolveLineNumberForOriginalTemplate(
                    It.IsAny<string>(),
                    It.Is<JToken>(templateContext.ExpandedTemplate, EqualityComparer<object>.Default),
                    It.Is<JToken>(templateContext.OriginalTemplate, EqualityComparer<object>.Default)))
                .Returns(expectedLineNumber);

            var ruleEngine = new JsonRuleEngine(mockLineResolver.Object);

            // Act
            var evaluationResults = ruleEngine.EvaluateRules(templateContext, rules).ToList();

            // Assert
            Assert.AreEqual(ruleEvaluationDefinitions.Length, evaluationResults.Count());
            Assert.AreEqual(numberOfExpectedPassedResults, evaluationResults.Count(result => result.Passed));
            for (int i = 0; i < ruleEvaluationDefinitions.Length; i++)
            {
                var evaluation = evaluationResults[i];
                Assert.AreEqual($"RuleName {i}", evaluation.RuleName);
                Assert.AreEqual(expectedFileId, evaluation.FileIdentifier);

                foreach (var result in evaluation.Results)
                {
                    Assert.AreEqual(expectedLineNumber, result.LineNumber);
                }
            }
        }

        [DataTestMethod]
        [DataRow(@"[{
                ""name"": ""RuleName 0"",
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
            }]", DisplayName = "Single structured expression")]
        [DataRow(@"[{
                ""name"": ""RuleName 0"",
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
            }]", DisplayName = "Nested structured expression")]
        public void EvaluateRules_ValidStructuredExpression_ReturnsExpectedEvaluations(string rules)
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

            TemplateContext templateContext = new TemplateContext
            {
                OriginalTemplate = template,
                ExpandedTemplate = template,
                IsMainTemplate = true,
                TemplateIdentifier = expectedFileId
            };

            // Setup mock line number resolver
            var mockLineResolver = new Mock<IJsonLineNumberResolver>();
            mockLineResolver.Setup(r =>
                r.ResolveLineNumberForOriginalTemplate(
                    It.IsAny<string>(),
                    It.Is<JToken>(templateContext.ExpandedTemplate, EqualityComparer<object>.Default),
                    It.Is<JToken>(templateContext.OriginalTemplate, EqualityComparer<object>.Default)))
                .Returns(expectedLineNumber);

            var ruleEngine = new JsonRuleEngine(mockLineResolver.Object);
            var evaluationResults = ruleEngine.EvaluateRules(templateContext, rules).ToList();

            Assert.AreEqual(1, evaluationResults.Count());
            Assert.AreEqual(1, evaluationResults.Count(evaluation => evaluation.Passed));
            for (int i = 0; i < 1; i++)
            {
                var evaluation = evaluationResults[i];
                Assert.AreEqual($"RuleName {i}", evaluation.RuleName);
                Assert.AreEqual(expectedFileId, evaluation.FileIdentifier);

                Assert.IsNull(evaluation.Results);

                AssertEvaluationsAndResultsAreAsExpected(evaluation, expectedLineNumber);
            }
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

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullLineNumberResolver_ThrowsException()
        {
            new JsonRuleEngine(null);
        }

        private string CreateRulesFromEvaluationDefinitions(string[] ruleEvaluationDefinitions)
        {
            string ruleSkeleton = @"{{
                ""name"": ""RuleName {0}"",
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