// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class AnyOfExpressionTests
    {
        [DataTestMethod]
        [DataRow(true, true, DisplayName = "AnyOf evaluates to true (true || true)")]
        [DataRow(true, false, DisplayName = "AnyOf evaluates to true (true || false)")]
        [DataRow(false, true, DisplayName = "AnyOf evaluates to true (false || true)")]
        [DataRow(false, false, DisplayName = "AnyOf evaluates to false (false || false)")]
        [DataRow(false, false, "ResourceProvider/resource", DisplayName = "AnyOf - scoped to resourceType - evaluates to false (false || false)")]
        [DataRow(false, false, "ResourceProvider/resource", "some.path", DisplayName = "AnyOf - scoped to resourceType and path - evaluates to false (false || false)")]
        public void Evaluate_TwoLeafExpressions_ExpectedResultIsReturned(bool evaluation1, bool evaluation2, string resourceType = null, string path = null)
        {
            // Arrange
            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            var mockLineResolver = new Mock<ILineNumberResolver>().Object;

            // This AnyOf will have 2 expressions
            var mockOperator1 = new Mock<LeafExpressionOperator>().Object;
            var mockOperator2 = new Mock<LeafExpressionOperator>().Object;

            var mockLeafExpression1 = new Mock<LeafExpression>(mockLineResolver, mockOperator1, new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path" });
            var mockLeafExpression2 = new Mock<LeafExpression>(mockLineResolver, mockOperator2, new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path" });

            var jsonRuleResult1 = new JsonRuleResult
            {
                Passed = evaluation1
            };

            var jsonRuleResult2 = new JsonRuleResult
            {
                Passed = evaluation2
            };

            mockJsonPathResolver
                .Setup(s => s.Resolve(It.IsAny<string>()))
                .Returns(new List<IJsonPathResolver> { mockJsonPathResolver.Object });

            if (!string.IsNullOrEmpty(resourceType))
            {
                mockJsonPathResolver
                    .Setup(s => s.ResolveResourceType(It.Is<string>(type => type == resourceType)))
                    .Returns(new List<IJsonPathResolver> { mockJsonPathResolver.Object });
            }

            var results1 = new JsonRuleResult[] { jsonRuleResult1 };
            var results2 = new JsonRuleResult[] { jsonRuleResult2 };

            mockLeafExpression1
                .Setup(s => s.Evaluate(mockJsonPathResolver.Object))
                .Returns(new JsonRuleEvaluation(mockLeafExpression1.Object, evaluation1, results1));

            mockLeafExpression2
                .Setup(s => s.Evaluate(mockJsonPathResolver.Object))
                .Returns(new JsonRuleEvaluation(mockLeafExpression2.Object, evaluation2, results2));

            var expressionArray = new Expression[] { mockLeafExpression1.Object, mockLeafExpression2.Object };

            var anyOfExpression = new AnyOfExpression(expressionArray, new ExpressionCommonProperties { ResourceType = resourceType, Path = path });

            // Act
            var anyOfEvaluation = anyOfExpression.Evaluate(mockJsonPathResolver.Object);

            // Assert
            bool expectedAnyOfEvaluation = evaluation1 || evaluation2;
            Assert.AreEqual(expectedAnyOfEvaluation, anyOfEvaluation.Passed);
            Assert.AreEqual(2, anyOfEvaluation.Evaluations.Count());
            Assert.IsTrue(anyOfEvaluation.HasResults);

            int expectedTrue = new[] { evaluation1, evaluation2 }.Count(e => e);
            int expectedFalse = 2 - expectedTrue;

            Assert.AreEqual(expectedTrue, anyOfEvaluation.EvaluationsEvaluatedTrue.Count());
            Assert.AreEqual(expectedFalse, anyOfEvaluation.EvaluationsEvaluatedFalse.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Evaluate_NullScope_ThrowsException()
        {
            new AnyOfExpression(new Expression[0], new ExpressionCommonProperties()).Evaluate(jsonScope: null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullExpressions_ThrowsException()
        {
            new AnyOfExpression(null, new ExpressionCommonProperties());
        }
    }
}
