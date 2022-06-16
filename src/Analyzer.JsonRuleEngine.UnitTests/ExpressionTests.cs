// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
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
                .Setup(r => r.ResolveResourceType(It.IsAny<string>(), null, null))
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
                    return new[] { new JsonRuleEvaluation(null, passed: false, result: new JsonRuleResult()) };
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

            mockExpression.Evaluate(mockPathResolver.Object, null);

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
                .Setup(r => r.ResolveResourceType(It.IsAny<string>(), null, null))
                .Returns(() => new[] { mockPathResolver.Object });

            bool whereConditionWasEvaluated = false;

            // Create a mock expression for the Where condition.
            // It will return an Evaluation that Passed, but contains no results (i.e. no paths were evaluated).
            var whereExpression = new MockExpression(new ExpressionCommonProperties())
            {
                EvaluationCallback = pathResolver =>
                {
                    whereConditionWasEvaluated = true;
                    return new[] { new JsonRuleEvaluation(null, passed: true, evaluations: Array.Empty<JsonRuleEvaluation>()) };
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

            mockExpression.Evaluate(mockPathResolver.Object, null);

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
                .Setup(r => r.ResolveResourceType(It.IsAny<string>(), null, null))
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
                    return new[] { new JsonRuleEvaluation(null, passed: true, result: new JsonRuleResult()) };
                }
            };

            // A top level mocked expression that contains a Where condition.
            var mockExpression = new MockExpression(new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path", Where = whereExpression })
            {
                EvaluationCallback = pathResolver =>
                {
                    // Track whether this was evaluated or not.
                    topLevelExpressionWasEvaluated = true;
                    return new[] { new JsonRuleEvaluation(null, passed: true, evaluations: Array.Empty<JsonRuleEvaluation>()) };
                }
            };

            mockExpression.Evaluate(mockPathResolver.Object, null);

            Assert.IsTrue(whereConditionWasEvaluated);
            Assert.IsTrue(topLevelExpressionWasEvaluated);
        }

        [TestMethod]
        public void EvaluateInternal_HasWhereCondition_LineNumberResolverNotPassed()
        {
            var mockPathResolver = new Mock<IJsonPathResolver>();
            mockPathResolver
                .Setup(r => r.Resolve(It.IsAny<string>()))
                .Returns(() => new[] { mockPathResolver.Object });
            mockPathResolver
                .Setup(r => r.ResolveResourceType(It.IsAny<string>(), null, null))
                .Returns(() => new[] { mockPathResolver.Object });

            bool lineNumberResolverWasAlwaysNull = true;

            var whereExpression = new Mock<Expression>(new ExpressionCommonProperties());
            whereExpression
                .Setup(w => w.Evaluate(It.IsAny<IJsonPathResolver>(), It.IsAny<ILineNumberResolver>()))
                .Returns((IJsonPathResolver pathResolver, ILineNumberResolver lineNumberResolver) =>
                {
                    // If a non-null ILineNumberResolver was passed to this Where condition, record it to assert later.
                    lineNumberResolverWasAlwaysNull &= lineNumberResolver == null;
                    return new[] { new JsonRuleEvaluation(null, passed: true, evaluations: Array.Empty<JsonRuleEvaluation>()) };
                });

            // A top level mocked expression that contains a Where condition.
            var mockExpression = new MockExpression(new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path", Where = whereExpression.Object })
            {
                EvaluationCallback = pathResolver =>
                {
                    return new[] { new JsonRuleEvaluation(null, passed: true, evaluations: Array.Empty<JsonRuleEvaluation>()) };
                }
            };

            // Evaluate scope - line number resolver should not be passed into Where condition
            mockExpression.Evaluate(mockPathResolver.Object, new Mock<ILineNumberResolver>().Object);

            Assert.IsTrue(lineNumberResolverWasAlwaysNull, "A non-null ILineNumberResolver was passed to a Where condition when it shouldn't have.");
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
        public void Constructor_NullCommonProperties_ClassPropertiesAreNull()
        {
            var expression = new MockExpression(null);
            Assert.IsNull(expression.Path);
            Assert.IsNull(expression.ResourceType);
            Assert.IsNull(expression.Where);
        }
    }
}
