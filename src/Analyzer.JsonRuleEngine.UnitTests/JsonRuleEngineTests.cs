// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine;
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
        public void Run_ValidInputs_ReeturnsExpectedResults(string[] evaluations, int numberOfExpectedPassedResults)
        {
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

            var rules = CreateRulesFromEvaluations(evaluations);


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
            var results = ruleEngine.EvaluateRules(templateContext, rules).ToList();

            Assert.AreEqual(evaluations.Length, results.Count());
            Assert.AreEqual(numberOfExpectedPassedResults, results.Count(result => result.Passed));
            for (int i = 0; i < evaluations.Length; i++)
            {
                var result = results[i];
                Assert.AreEqual($"RuleName {i}", result.RuleName);
                Assert.AreEqual(expectedFileId, result.FileIdentifier);
                Assert.AreEqual(expectedLineNumber, result.LineNumber);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullLineNumberResolver_ThrowsException()
        {
            new JsonRuleEngine(null);
        }

        private string CreateRulesFromEvaluations(string[] evaluations)
        {
            string ruleSkeleton = @"{{
                ""name"": ""RuleName {0}"",
                ""description"": ""Rule description {0}"",
                ""recommendation"": ""Recommendation {0}"",
                ""helpUri"": ""Uri {0}"",
                ""evaluation"": {1}
            }}";

            List<string> rules = new List<string>();
            for (int i = 0; i < evaluations.Length; i++)
            {
                var evaluation = evaluations[i];
                rules.Add(string.Format(ruleSkeleton, i, evaluation));
            }

            return $"[{string.Join(",", rules)}]";
        }
    }
}