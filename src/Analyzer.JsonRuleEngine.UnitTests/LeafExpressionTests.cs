// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Moq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class LeafExpressionTests
    {
        [DataTestMethod]
        [DataRow(null, "", DisplayName = "No resource type and an empty path")]
        [DataRow(null, "some.json.path", DisplayName = "No resource type and a path")]
        [DataRow("Namespace/resourceType", "", DisplayName = "A resource type and an empty path")]
        [DataRow("Namespace/resourceType", "some.json.path", DisplayName = "A resource type and a path")]
        public void Constructor_ValidParameters_ConstructedCorrectly(string resourceType, string path)
        {
            var mockOperator = new Mock<LeafExpressionOperator>().Object;
            var leafExpression = new LeafExpression(resourceType, path, null, mockOperator);

            Assert.AreEqual(resourceType, leafExpression.ResourceType);
            Assert.AreEqual(path, leafExpression.Path);
            Assert.AreEqual(mockOperator, leafExpression.Operator);
        }

        [DataTestMethod]
        [DataRow(null, "", false, DisplayName = "No resource type, empty path, operator evaluates to false")]
        [DataRow(null, "some.path", true, DisplayName = "No resource type, valid path, operator evaluates to true")]
        [DataRow("someResource/type", "some.path", true, DisplayName = "Resource Type specified, valid path, operator evaluates to true")]
        public void Evaluate_ValidScope_ReturnsResultsOfOperatorEvaluation(string resourceType, string path, bool expectedEvaluationResult)
        {
            // Arrange
            // JObject to evaluate, in this test, this is a subset of an ARM template
            var jsonToEvaluate = JObject.Parse("{ \"property\": \"value\" }");
            var expectedPathEvaluated = "expectedPath";

            // Setting up the Mock JsonPathResolvers to return the expected values when JToken and Resolve are called
            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            var mockResourcesResolved = new Mock<IJsonPathResolver>();

            // The JToken property should return the JObject to evaluate
            mockJsonPathResolver
                .Setup(s => s.JToken)
                .Returns(jsonToEvaluate);

            // Resolve for the provided json path should return a JsonPathResolver.
            // Both mocks need to be prepared to return a value.
            // We can just reuse mockJsonPathResolver in both cases.
            mockJsonPathResolver
                .Setup(s => s.Resolve(It.Is<string>(p => string.Equals(p, path))))
                .Returns(new[] { mockJsonPathResolver.Object });
            mockResourcesResolved
                .Setup(s => s.Resolve(It.Is<string>(p => string.Equals(p, path))))
                .Returns(new[] { mockJsonPathResolver.Object });

            // Setup the mock resolver to return a JSON path
            mockJsonPathResolver
                .Setup(s => s.Path)
                .Returns(expectedPathEvaluated);

            // ResolveResourceType for the provided resource type should return a JsonPathResolver.
            // Return the mock specifically for testing the call to ResolveResourceType.
            mockJsonPathResolver
                .Setup(s => s.ResolveResourceType(It.Is<string>(r => string.Equals(r, resourceType))))
                .Returns(new[] { mockResourcesResolved.Object });

            // EvaluateExpression for the provided scope should return the expected evaluationResult
            var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>();
            mockLeafExpressionOperator
                .Setup(o => o.EvaluateExpression(It.Is<JToken>(token => token == jsonToEvaluate)))
                .Returns(expectedEvaluationResult);

            var leafExpression = new LeafExpression(resourceType, path, null, mockLeafExpressionOperator.Object);

            // Act
            var evaluation = leafExpression.Evaluate(jsonScope: mockJsonPathResolver.Object);
            var results = evaluation.Results.ToList();

            // Assert
            // Verify actions on resolvers.

            // If a resource type is passed, it should resolve for the resource type, and the path should be resolved on the mock resource type.
            // If no resource type is passed, it should resolve the path directly and not use the mock resource type.
            // ResolveResourceType should never be called on the mock returned from resolving resource types already.
            mockJsonPathResolver.Verify(s => s.Resolve(It.Is<string>(p => string.Equals(p, path))), Times.Exactly(resourceType == null ? 1 : 0));
            mockJsonPathResolver.Verify(s => s.ResolveResourceType(It.Is<string>(r => string.Equals(r, resourceType))), Times.Exactly(resourceType == null ? 0 : 1));
            mockResourcesResolved.Verify(s => s.Resolve(It.Is<string>(p => string.Equals(p, path))), Times.Exactly(resourceType == null ? 0 : 1));
            mockResourcesResolved.Verify(s => s.ResolveResourceType(It.IsAny<string>()), Times.Never);

            // The original mock is returned from both mocks when calling Resolve for a path, so the JToken should always come from it.
            mockJsonPathResolver.Verify(s => s.JToken, Times.Once);
            mockResourcesResolved.Verify(s => s.JToken, Times.Never);

            mockLeafExpressionOperator.Verify(o => o.EvaluateExpression(It.Is<JToken>(token => token == jsonToEvaluate)), Times.Once);

            Assert.AreEqual(expectedEvaluationResult, evaluation.Passed);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(expectedEvaluationResult, results.First().Passed);
            Assert.AreEqual(expectedPathEvaluated, (results.First() as JsonRuleResult).JsonPath);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullPath_ThrowsException()
        {
            new LeafExpression("resourceType", null, null, new ExistsOperator(true, false));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullOperator_ThrowsException()
        {
            new LeafExpression("resourceType", "path", null, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Evaluate_NullScope_ThrowsException()
        {
            var leafExpression = new LeafExpression(null, "path", null, new HasValueOperator(true, false));
            leafExpression.Evaluate(jsonScope: null).ToList();
        }
    }
}
