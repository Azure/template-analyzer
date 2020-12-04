// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Armory.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class LeafExpressionTests
    {
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
        [DataRow(null, null, true, DisplayName = "No resource type or path, operator evaluates to true")]
        [DataRow(null, "", false, DisplayName = "No resource type, empty path, operator evaluates to false")]
        [DataRow(null, "some.path", true, DisplayName = "No resource type, valid path, operator evaluates to true")]
        public void Evaluate_ValidScope_ReturnsResultsOfOperatorEvaluation(string resourceType, string path, bool evaluationResult)
        {
            var ruleDefinition = new RuleDefinition
            {
                Name = "testRule",
                Description = "test rule",
                Recommendation = "test recommendation",
                HelpUri = "https://helpUri"
            };

            var jsonToEvaluate = JObject.Parse("{ \"property\": \"value\" }");
            var mockScope = new Mock<IJsonPathResolver>();
            mockScope
                .Setup(s => s.JToken)
                .Returns(jsonToEvaluate);
            mockScope
                .Setup(s => s.Resolve(It.Is<string>(p => string.Equals(p, path))))
                .Returns(() => new[] { mockScope.Object });

            var mockOperator = new Mock<LeafExpressionOperator>();
            mockOperator
                .Setup(o => o.EvaluateExpression(It.Is<JToken>(token => token == jsonToEvaluate)))
                .Returns(evaluationResult);

            var leafExpression = new LeafExpression(ruleDefinition, resourceType, path, mockOperator.Object);
            var results = leafExpression.Evaluate(jsonScope: mockScope.Object).ToList();

            mockScope.Verify(s => s.Resolve(It.Is<string>(p => string.Equals(p, path))), Times.Once);
            mockScope.Verify(s => s.JToken, Times.Once);

            mockOperator.Verify(o => o.EvaluateExpression(It.Is<JToken>(token => token == jsonToEvaluate)), Times.Once);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(evaluationResult, results[0].Passed);
            Assert.AreEqual(ruleDefinition, results[0].RuleDefinition);
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
