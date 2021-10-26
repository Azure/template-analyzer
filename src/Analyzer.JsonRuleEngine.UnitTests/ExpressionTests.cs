// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
    public class ExpressionTests
    {
        [TestMethod]
        public void Evaluate_WhereConditionFalse_PathNotEvaluated()
        {
            var mockPathResolver = new Mock<IJsonPathResolver>();
            mockPathResolver
                .Setup(r => r.Resolve(It.IsAny<string>()))
                .Returns(() => new[] { mockPathResolver.Object });
            mockPathResolver
                .Setup(r => r.ResolveResourceType(It.IsAny<string>()))
                .Returns(() => new[] { mockPathResolver.Object });

            bool whereConditionWasEvaluated = false;

            // Create a mock expression for the Where condition.
            // It will return an Evaluation that has results, but Passed is false.
            var whereExpression = new MockExpression(new ExpressionCommonProperties())
            {
                // This will only be executed if this where condition is evaluated.
                EvaluationCallback = pathResolver =>
                {
                    whereConditionWasEvaluated = true;
                    return new JsonRuleEvaluation(null, passed: false, results: new[] { new JsonRuleResult() });
                }
            };

            // A top level mocked expression that contains a Where condition.
            var mockExpression = new MockExpression(new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path", Where = whereExpression })
            {
                // This will only be executed if the expression is evaluated.
                EvaluationCallback = pathResolver =>
                {
                    // This expression should not evaluate anything because the Where condition does not pass
                    Assert.Fail("Top-level expression was evaluated even though the Where condition did not pass.");
                    return null;
                }
            };

            mockExpression.Evaluate(mockPathResolver.Object, new Mock<ILineNumberResolver>().Object);

            // whereConditionWasEvaluated will only be true if 'whereExpression'
            // is evaluated in the base Expression class.
            Assert.IsTrue(whereConditionWasEvaluated);
        }

        [TestMethod]
        public void Evaluate_WhereConditionTrueWithNoResults_PathNotEvaluated()
        {
            var mockPathResolver = new Mock<IJsonPathResolver>();
            mockPathResolver
                .Setup(r => r.Resolve(It.IsAny<string>()))
                .Returns(() => new[] { mockPathResolver.Object });
            mockPathResolver
                .Setup(r => r.ResolveResourceType(It.IsAny<string>()))
                .Returns(() => new[] { mockPathResolver.Object });

            bool whereConditionWasEvaluated = false;

            // Create a mock expression for the Where condition.
            // It will return an Evaluation that Passed, but contains no results (i.e. no paths were evaluated).
            var whereExpression = new MockExpression(new ExpressionCommonProperties())
            {
                EvaluationCallback = pathResolver =>
                {
                    whereConditionWasEvaluated = true;
                    return new JsonRuleEvaluation(null, passed: true, results: Array.Empty<JsonRuleResult>());
                }
            };

            // A top level mocked expression that contains a Where condition.
            var mockExpression = new MockExpression(new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path", Where = whereExpression })
            {
                EvaluationCallback = pathResolver =>
                {
                    // This expression should not evaluate anything because the Where condition does not have any results.
                    Assert.Fail("Top-level expression was evaluated even though the Where condition contains no results.");
                    return null;
                }
            };

            mockExpression.Evaluate(mockPathResolver.Object, new Mock<ILineNumberResolver>().Object);

            Assert.IsTrue(whereConditionWasEvaluated);
        }

        [TestMethod]
        public void Evaluate_WhereConditionTrue_PathIsEvaluated()
        {
            var mockPathResolver = new Mock<IJsonPathResolver>();
            mockPathResolver
                .Setup(r => r.Resolve(It.IsAny<string>()))
                .Returns(() => new[] { mockPathResolver.Object });
            mockPathResolver
                .Setup(r => r.ResolveResourceType(It.IsAny<string>()))
                .Returns(() => new[] { mockPathResolver.Object });

            bool whereConditionWasEvaluated = false;
            bool topLevelExpressionWasEvaluated = false;

            // Create a mock expression for the Where condition.
            // It will return an Evaluation that Passed, but contains no results (i.e. no paths were evaluated).
            var whereExpression = new MockExpression(new ExpressionCommonProperties())
            {
                EvaluationCallback = pathResolver =>
                {
                    whereConditionWasEvaluated = true;
                    return new JsonRuleEvaluation(null, passed: true, results: new[] { new JsonRuleResult() });
                }
            };

            // A top level mocked expression that contains a Where condition.
            var mockExpression = new MockExpression(new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path", Where = whereExpression })
            {
                EvaluationCallback = pathResolver =>
                {
                    // Track whether this was evaluated or not.
                    topLevelExpressionWasEvaluated = true;
                    return new JsonRuleEvaluation(null, passed: true, results: Array.Empty<JsonRuleResult>());
                }
            };

            mockExpression.Evaluate(mockPathResolver.Object, new Mock<ILineNumberResolver>().Object);

            Assert.IsTrue(whereConditionWasEvaluated);
            Assert.IsTrue(topLevelExpressionWasEvaluated);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EvaluateInternalGetEvaluation_NullScope_ThrowsException()
        {
            // Calls EvaluateInternal with Func<IJsonPathResolver, JsonRuleEvaluation>
            new MockExpression(new ExpressionCommonProperties { Path = "path" })
                .Evaluate(null, new Mock<ILineNumberResolver>().Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EvaluateInternalGetEvaluation_NullLineNumberResolver_ThrowsException()
        {
            // Calls EvaluateInternal with Func<IJsonPathResolver, JsonRuleEvaluation>
            new MockExpression(new ExpressionCommonProperties { Path = "path" })
                .Evaluate(new Mock<IJsonPathResolver>().Object, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EvaluateInternalGetResult_NullScope_ThrowsException()
        {
            // Calls EvaluateInternal with Func<IJsonPathResolver, JsonRuleResult>
            var expression = new MockExpression(new ExpressionCommonProperties { Path = "path" });
            expression.ResultsCallback = r => (JsonRuleResult)null;
            expression.Evaluate(null, new Mock<ILineNumberResolver>().Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EvaluateInternalGetResult_NullLineNumberResolver_ThrowsException()
        {
            // Calls EvaluateInternal with Func<IJsonPathResolver, JsonRuleResult>
            var expression = new MockExpression(new ExpressionCommonProperties { Path = "path" });
            expression.ResultsCallback = r => (JsonRuleResult)null;
            expression.Evaluate(new Mock<IJsonPathResolver>().Object, null);
        }

        [TestMethod]
        public void Constructor_NullCommonProperties_ClassPropertiesAreNull()
        {
            var expression = new MockExpression(null);
            Assert.IsNull(expression.Path);
            Assert.IsNull(expression.ResourceType);
            Assert.IsNull(expression.Where);
        }
    }
}
