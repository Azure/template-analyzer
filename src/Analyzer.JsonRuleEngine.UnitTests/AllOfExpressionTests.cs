// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class AllOfExpressionTests
    {
        [DataTestMethod]
        [DataRow(true, true, DisplayName = "AllOf evaluates to true")]
        [DataRow(true, false, DisplayName = "AllOf evaluates to false")]
        [DataRow(false, false, DisplayName = "AllOf evaluates to false")]
        [DataRow(false, false, "ResourceProvider/resource", DisplayName = "AllOf - scoped to resourceType - evaluates to true")]
        public void Evaluate_TwoLeafExpressions_ExpectedResultIsReturned(bool hasValueEvaluation, bool equalsEvaluation, string resourceType = null)
        {
            // Arrange
            var mockJsonPathResolver = new Mock<IJsonPathResolver>();

            // This AllOf will have one hasValue expression and one equals expression
            var mockEqualsOperator = new Mock<LeafExpressionOperator>().Object;
            var mockHasValueOperator = new Mock<LeafExpressionOperator>().Object;

            var mockHasValueLeafExpression = new Mock<LeafExpression>(new object[] { "ResourceProvider/resource", "some.path", mockHasValueOperator });
            var mockEqualsLeafExpression = new Mock<LeafExpression>(new object[] { "ResourceProvider/resource", "some.path", mockEqualsOperator });

            var hasValueJsonRuleResult = new JsonRuleResult
            {
                Passed = hasValueEvaluation
            };

            var equalsJsonRuleResult = new JsonRuleResult
            {
                Passed = equalsEvaluation
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

            var hasValueResults = new JsonRuleResult[] { hasValueJsonRuleResult };
            var equalsResults = new JsonRuleResult[] { equalsJsonRuleResult };

            mockHasValueLeafExpression
                .Setup(s => s.Evaluate(mockJsonPathResolver.Object))
                .Returns(new Evaluation(hasValueEvaluation, hasValueResults));

            mockEqualsLeafExpression
                .Setup(s => s.Evaluate(mockJsonPathResolver.Object))
                .Returns(new Evaluation(equalsEvaluation, equalsResults));

            var expressionArray = new Expression[] { mockHasValueLeafExpression.Object, mockEqualsLeafExpression.Object };

            var allOfExpression = new AllOfExpression(expressionArray, resourceType);

            // Act
            var allOfEvaluation = allOfExpression.Evaluate(mockJsonPathResolver.Object);

            // Assert
            bool expectedAllOfEvaluation = hasValueEvaluation && equalsEvaluation;
            Assert.AreEqual(expectedAllOfEvaluation, allOfEvaluation.Passed);
            Assert.AreEqual(2, allOfEvaluation.Results.Count());
            int expectedTrue = 0;
            int expectedFalse = 0;

            if (hasValueEvaluation)
                expectedTrue++;
            else
                expectedFalse++;

            if (equalsEvaluation)
                expectedTrue++;
            else
                expectedFalse++;

            Assert.AreEqual(expectedTrue, allOfEvaluation.GetResultsEvaluatedTrue().Count());
            Assert.AreEqual(expectedFalse, allOfEvaluation.GetResultsEvaluatedFalse().Count());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Evaluate_NullScope_ThrowsException()
        {
            var allOfExpression = new AllOfExpression(new Expression[] { });
            allOfExpression.Evaluate(jsonScope: null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullExpressions_ThrowsException()
        {
            new AllOfExpression(null);
        }
    }
}
