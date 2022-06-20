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
using static Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests.TestUtilities;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class StructuredExpressionTests
    {
        private readonly Func<bool, bool, bool> DummyOperation = (x, y) => x ^ y;

        // tests functionality of AllOfExpression
        [DataTestMethod]
        [DataRow(true, true, DisplayName = "Evaluates to true (true && true)")]
        [DataRow(true, false, DisplayName = "Evaluates to false (true && false)")]
        [DataRow(false, true, DisplayName = "Evaluates to false (false && true)")]
        [DataRow(false, false, DisplayName = "Evaluates to false (false && false)")]
        [DataRow(false, false, "ResourceProvider/resource", DisplayName = "Scoped to resourceType - Evaluates to false (false && false)")]
        [DataRow(false, false, "ResourceProvider/resource", "some.path", DisplayName = "Scoped to resourceType and path - Evaluates to false (false && false)")]
        public void Evaluate_TwoLeafExpressions_ExpectedResultIsReturnedForAndOperations(bool evaluation1, bool evaluation2, string resourceType = null, string path = null)
        {
            Evaluate_TwoLeafExpressions(evaluation1, evaluation2, (x, y) => x && y, resourceType, path);
        }

        // tests functionality of AnyOfExpression
        [DataTestMethod]
        [DataRow(true, true, DisplayName = "Evaluates to true (true || true)")]
        [DataRow(true, false, DisplayName = "Evaluates to true (true || false)")]
        [DataRow(false, true, DisplayName = "Evaluates to true (false || true)")]
        [DataRow(false, false, DisplayName = "Evaluates to false (false || false)")]
        [DataRow(false, false, "ResourceProvider/resource", DisplayName = "Scoped to resourceType - Evaluates to false (false || false)")]
        [DataRow(false, false, "ResourceProvider/resource", "some.path", DisplayName = "Scoped to resourceType and path - Evaluates to false (false || false)")]
        public void Evaluate_TwoLeafExpressions_ExpectedResultIsReturnedForOrOperations(bool evaluation1, bool evaluation2, string resourceType = null, string path = null)
        {
            Evaluate_TwoLeafExpressions(evaluation1, evaluation2, (x, y) => x || y, resourceType, path);
        }

        private static void Evaluate_TwoLeafExpressions(bool evaluation1, bool evaluation2, Func<bool, bool, bool> operation, string resourceType = null, string path = null)
        {
            // Arrange
            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            var mockLineResolver = new Mock<ILineNumberResolver>().Object;

            // This AllOf will have 2 expressions
            var mockOperator1 = new Mock<LeafExpressionOperator>().Object;
            var mockOperator2 = new Mock<LeafExpressionOperator>().Object;

            var mockLeafExpression1 = new Mock<LeafExpression>(mockOperator1, new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path" });
            var mockLeafExpression2 = new Mock<LeafExpression>(mockOperator2, new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path" });

            var jsonRuleResult1 = new JsonRuleResult
            {
                Passed = evaluation1
            };

            var jsonRuleResult2 = new JsonRuleResult
            {
                Passed = evaluation2
            };

            mockJsonPathResolver
                .Setup(s => s.Resolve(It.Is<string>(path => path == "some.path")))
                .Returns(new List<IJsonPathResolver> { mockJsonPathResolver.Object });

            if (!string.IsNullOrEmpty(resourceType))
            {
                mockJsonPathResolver
                    .Setup(s => s.ResolveResourceType(It.Is<string>(type => type == resourceType)))
                    .Returns(new List<IJsonPathResolver> { mockJsonPathResolver.Object });
            }

            mockLeafExpression1
                .Setup(s => s.Evaluate(mockJsonPathResolver.Object, mockLineResolver))
                .Returns(new[] { new JsonRuleEvaluation(mockLeafExpression1.Object, evaluation1, jsonRuleResult1) });

            mockLeafExpression2
                .Setup(s => s.Evaluate(mockJsonPathResolver.Object, mockLineResolver))
                .Returns(new[] { new JsonRuleEvaluation(mockLeafExpression2.Object, evaluation2, jsonRuleResult2) });

            var expressionArray = new Expression[] { mockLeafExpression1.Object, mockLeafExpression2.Object };

            var structuredExpression = new StructuredExpression(expressionArray, operation, new ExpressionCommonProperties { ResourceType = resourceType, Path = path });

            bool expectedCompoundEvaluation = operation(evaluation1, evaluation2);

            // Act
            var structuredEvaluationOutcome = structuredExpression.Evaluate(mockJsonPathResolver.Object, mockLineResolver).ToList();

            // Assert
            Assert.AreEqual(1, structuredEvaluationOutcome.Count);

            var structuredEvaluation = structuredEvaluationOutcome[0];
            Assert.AreEqual(expectedCompoundEvaluation, structuredEvaluation.Passed);
            Assert.AreEqual(2, structuredEvaluation.Evaluations.Count());
            Assert.IsTrue(structuredEvaluation.HasResults);

            int expectedTrue = new[] { evaluation1, evaluation2 }.Count(e => e);
            int expectedFalse = 2 - expectedTrue;

            Assert.AreEqual(expectedTrue, structuredEvaluation.EvaluationsEvaluatedTrue.Count());
            Assert.AreEqual(expectedFalse, structuredEvaluation.EvaluationsEvaluatedFalse.Count());

            foreach (var evaluation in structuredEvaluation.Evaluations)
            {
                // Assert all leaf expressions have results and no evaluations
                Assert.IsTrue(evaluation.HasResults);
                Assert.AreEqual(0, evaluation.Evaluations.Count());
            }
        }

        [DataTestMethod]
        [DataRow(null, true, true, DisplayName = "First expression not evaluated, second expression passes, overall result is pass")]
        [DataRow(null, false, false, DisplayName = "First expression not evaluated, second expression fails, overall result is fail")]
        [DataRow(true, null, true, DisplayName = "First expression passes, second expression not evaluated, overall result is pass")]
        [DataRow(null, null, true, DisplayName = "All expressions not evaluated, overall result is pass")]
        public void Evaluate_OneExpressionNotEvaluated_ResultIsTakenFromOtherExpression(bool? firstExpressionPass, bool? secondExpressionPass, bool overallPass)
        {
            string evaluatedPath = "some.evaluated.path";
            string notEvaluatedPath = "path.not.evaluated";

            // Arrange
            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            mockJsonPathResolver
                .Setup(r => r.Resolve(It.Is<string>(path => evaluatedPath.Equals(path))))
                .Returns(() => new[] { mockJsonPathResolver.Object });

            var mockLineResolver = new Mock<ILineNumberResolver>().Object;

            // Create 2 expressions for the AnyOf.
            // Whether each is evaluated or not is determined by the path passed to each.

            var mockLeafExpression1 = new MockExpression(new ExpressionCommonProperties { Path = firstExpressionPass.HasValue ? evaluatedPath : notEvaluatedPath })
            {
                EvaluationCallback = pathResolver => firstExpressionPass.HasValue
                        ? new[] { new JsonRuleEvaluation(null, firstExpressionPass.Value, new JsonRuleResult { Passed = firstExpressionPass.Value }) }
                        : Enumerable.Empty<JsonRuleEvaluation>()
            };

            var mockLeafExpression2 = new MockExpression(new ExpressionCommonProperties { Path = secondExpressionPass.HasValue ? evaluatedPath : notEvaluatedPath })
            {
                EvaluationCallback = pathResolver => secondExpressionPass.HasValue
                        ? new[] { new JsonRuleEvaluation(null, secondExpressionPass.Value, new JsonRuleResult { Passed = secondExpressionPass.Value }) }
                        : Enumerable.Empty<JsonRuleEvaluation>()
            };

            var structuredExpression = new StructuredExpression(
                new Expression[] { mockLeafExpression1, mockLeafExpression2 },
                DummyOperation,
                new ExpressionCommonProperties());

            var expectedResults = new[] { firstExpressionPass, secondExpressionPass };

            // Act
            var structuredEvaluationOutcome = structuredExpression.Evaluate(mockJsonPathResolver.Object, mockLineResolver).ToList();

            // Assert
            Assert.AreEqual(1, structuredEvaluationOutcome.Count);

            var structuredEvaluation = structuredEvaluationOutcome[0];
            Assert.AreEqual(overallPass, structuredEvaluation.Passed);
            Assert.AreEqual(expectedResults.Count(r => r.HasValue), structuredEvaluation.Evaluations.Count());
            Assert.AreEqual(expectedResults.Any(r => r.HasValue), structuredEvaluation.HasResults);

            Assert.AreEqual(expectedResults.Count(r => r.HasValue && r.Value), structuredEvaluation.EvaluationsEvaluatedTrue.Count());
            Assert.AreEqual(expectedResults.Count(r => r.HasValue && !r.Value), structuredEvaluation.EvaluationsEvaluatedFalse.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullExpressions_ThrowsException()
        {
            new StructuredExpression(null, DummyOperation, new ExpressionCommonProperties());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullOperation_ThrowsException()
        {
            new StructuredExpression(Array.Empty<Expression>(), null, new ExpressionCommonProperties());
        }
    }
}
