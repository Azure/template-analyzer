// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Armory.Utilities;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Armory.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class LeafExpressionTests
    {
        private const string singleField = "{ \"property\": \"value\" }";
        private const string singleResource = @"{ ""resources"": [ { ""type"": ""Microsoft.ResourceProvider/type"", ""properties"": { ""some"": { ""path"": ""value"" } } } ] }";

        [DataTestMethod]
        [DataRow(null, null, DisplayName = "No resource type or path")]
        [DataRow(null, "", DisplayName = "No resource type and an empty path")]
        [DataRow("Namespace/resourceType", null, DisplayName = "A resource type and no path")]
        [DataRow("Namespace/resourceType", "", DisplayName = "A resource type and an empty path")]
        [DataRow("Namespace/resourceType", "some.json.path", DisplayName = "A resource type and a path")]
        public void Constructor_ValidParameters_ConstructedCorrectly(string resourceType, string path)
        {
            var mockOperator = new Mock<LeafExpressionOperator>().Object;
            var leafExpression = new LeafExpression(new RuleDefinition(), resourceType, path, mockOperator);

            Assert.AreEqual(resourceType, leafExpression.ResourceType);
            Assert.AreEqual(path, leafExpression.Path);
            Assert.AreEqual(mockOperator, leafExpression.Operator);
        }

        [DataTestMethod]
        [DataRow(null, null, true, singleField, null, 1, DisplayName = "No resource type or path, operator evaluates to true")]
        [DataRow(null, "", false, singleField, null, 1, DisplayName = "No resource type, empty path, operator evaluates to false")]
        [DataRow(null, "some.path", true, singleField, null, 1, DisplayName = "No resource type, valid path, operator evaluates to true")]
        [DataRow("Microsoft.ResourceProvider/type", "properties.some.path", true, singleResource, "resources[0].properties.some.path", 0, DisplayName = "Resource Type specified with single matching resource, valid path, operator evaluates to true")]
        public void Evaluate_ValidScope_ReturnsResultsOfOperatorEvaluation(string resourceType, string path, bool expectedEvaluationResult, string jtoken, string absolutePathForEvaluateExpression, int timesResolveIsCalledOnOriginalObject)
        {
            // Arrange
            var ruleDefinition = new RuleDefinition
            {
                Name = "testRule",
                Description = "test rule",
                Recommendation = "test recommendation",
                HelpUri = "https://helpUri"
            };

            // JObject to evaluate, in this test, this is a subset of an ARM template
            var jsonToEvaluate = JObject.Parse(jtoken);

            // Setting up the Mock JsonPathResolver to return the expected values when JToken and Resolve are called
            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            // The JToken property should return the JObject to evaluate
            mockJsonPathResolver
                .Setup(s => s.JToken)
                .Returns(jsonToEvaluate);
            // Resolve for the provided json path should return the JsonPathResolver
            mockJsonPathResolver
                .Setup(s => s.Resolve(It.Is<string>(p => string.Equals(p, path))))
                .Returns(() => new[] { mockJsonPathResolver.Object });

            // Call a helper function to get the appropriate scope of the JObject
            // absolutePathForEvaluateExpression is null when resourceType is not defined
            // When resourceType is defined, absolutePathForEvaluateExpression represents the absolute
            // json path of the path parameter specified
            var jTokenExpectedInEvaluateExpression = GetRelevantJTokenScope(jsonToEvaluate, absolutePathForEvaluateExpression);

            // EvaluateExpression for the provided scope should return the expected evaluationResult
            var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>();
            mockLeafExpressionOperator
                .Setup(o => o.EvaluateExpression(It.Is<JToken>(token => token == jTokenExpectedInEvaluateExpression)))
                .Returns(expectedEvaluationResult);

            var leafExpression = new LeafExpression(ruleDefinition, resourceType, path, mockLeafExpressionOperator.Object);

            // Act
            var results = leafExpression.Evaluate(jsonScope: mockJsonPathResolver.Object).ToList();

            // Assert
            // Verify the number of time Resolve, JToken, and EvaluateExpression were called
            mockJsonPathResolver.Verify(s => s.Resolve(It.Is<string>(p => string.Equals(p, path))), Times.Exactly(timesResolveIsCalledOnOriginalObject));
            mockJsonPathResolver.Verify(s => s.JToken, Times.Once);

            mockLeafExpressionOperator.Verify(o => o.EvaluateExpression(It.Is<JToken>(token => token == jTokenExpectedInEvaluateExpression)), Times.Once);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(expectedEvaluationResult, results.First().Passed);
            Assert.AreEqual(ruleDefinition, results.First().RuleDefinition);
        }

        private JToken GetRelevantJTokenScope(JToken jToken, string path)
        {
            return path == null ? jToken : jToken.InsensitiveToken(path);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullOperator_ThrowsException()
        {
            new LeafExpression(new RuleDefinition(), null, null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullRuleDefinition_ThrowsException()
        {
            new LeafExpression(null, null, null, new ExistsOperator(true, false));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Evaluate_NullScope_ThrowsException()
        {
            var leafExpression = new LeafExpression(new RuleDefinition(), null, null, new HasValueOperator(true, false));
            leafExpression.Evaluate(jsonScope: null).ToList();
        }
    }
}
