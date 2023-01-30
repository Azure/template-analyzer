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
    public class NotExpressionTests
    {
        [DataTestMethod]
        [DataRow(null, "", false, DisplayName = "Not evaluates to false")]
        [DataRow(null, "some.path", true, DisplayName = "Not evaluates to true")]
        [DataRow("someResource/type", "some.path", true, DisplayName = "Resource Type specified, valid path, not evaluates to true")]
        public void Evaluate_SingleLeafExpression_ReturnsResultsOfOperatorEvaluation(string resourceType, string path, bool expectedEvaluationResult)
        {
            // Arrange
            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            var mockLineResolver = new Mock<ISourceLocationResolver>().Object;

            var mockOperator = new Mock<LeafExpressionOperator>().Object;
            mockOperator.IsNegative = true;

            var mockLeafExpression = new Mock<LeafExpression>(mockOperator, new ExpressionCommonProperties { ResourceType = resourceType, Path = path });
            var result = new Result(expectedEvaluationResult);

            mockJsonPathResolver
                .Setup(s => s.Resolve(It.Is<string>(p => p == path)))
                .Returns(new List<IJsonPathResolver> { mockJsonPathResolver.Object });

            if (!string.IsNullOrEmpty(resourceType))
            {
                mockJsonPathResolver
                    .Setup(s => s.ResolveResourceType(It.Is<string>(type => type == resourceType)))
                    .Returns(new List<IJsonPathResolver> { mockJsonPathResolver.Object });
            }

            mockLeafExpression
                .Setup(s => s.Evaluate(mockJsonPathResolver.Object, mockLineResolver))
                .Returns(new[] { new JsonRuleEvaluation(mockLeafExpression.Object, expectedEvaluationResult, result) });

            var notExpression = new NotExpression(mockLeafExpression.Object, new ExpressionCommonProperties { ResourceType = resourceType, Path = path });

            // Act
            var evaluationOutcome = notExpression.Evaluate(jsonScope: mockJsonPathResolver.Object, mockLineResolver).ToList();

            // Assert
            Assert.AreEqual(1, evaluationOutcome.Count);

            var evaluation = evaluationOutcome[0];
            Assert.AreEqual(expectedEvaluationResult, evaluation.Passed);
            Assert.AreEqual(expectedEvaluationResult, evaluation.Result.Passed);

            Assert.IsTrue(mockLeafExpression.Object.Operator.IsNegative);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullExpressions_ThrowsException()
        {
            new NotExpression(null, new ExpressionCommonProperties());
        }
    }
}
