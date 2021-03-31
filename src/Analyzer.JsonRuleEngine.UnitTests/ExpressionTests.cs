using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class ExpressionTests
    {
        /// <summary>
        /// A mock implementation of an <see cref="Expression"/> for testing internal methods.
        /// </summary>
        private class MockExpression : Expression
        {
            private Func<IJsonPathResolver, JsonRuleEvaluation> evaluationCallback;

            public MockExpression(ExpressionCommonProperties commonProperties, Func<IJsonPathResolver, JsonRuleEvaluation> evaluationCallback)
                : base(commonProperties)
            {
                this.evaluationCallback = evaluationCallback;
            }

            public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
            {
                return base.EvaluateInternal(jsonScope, evaluationCallback);
            }
        }

        [DataTestMethod]
        [DataRow(null, "some.path", false, true, false, DisplayName = "No resource type, a path, 1/3 paths evaluated")]
        public void Evaluate_WhereCondition_CorrectPathsEvaluated(string resourceType, string path, params bool[] resultsForWhereCondition)
        {
            // An array to track what paths were evaluated
            bool[] actualEvaluations = new bool[resultsForWhereCondition.Length];

            int numPathsEvaluated = 0;

            var mockPathResolvers_subLevel = GenerateMockPathResolvers(resultsForWhereCondition.Length);

            var mockPathResolver_topLevel = new Mock<IJsonPathResolver>();
            mockPathResolver_topLevel
                .Setup(r => r.Resolve(It.Is<string>(p => string.Equals(p, path))))
                .Returns(mockPathResolvers_subLevel);

            // Create a mock expression for the Where condition.
            // It will return an Evaluation for each boolean in resultsForWhereCondition, with
            // the corresponding boolean value for its Passed property.
            var mockExpression_Where = new MockExpression(new ExpressionCommonProperties(), pathResolver =>
            {
                int resultsIndex = mockPathResolvers_subLevel.IndexOf(pathResolver);
                return new JsonRuleEvaluation(null, resultsForWhereCondition[resultsIndex], new[] { new JsonRuleResult() });
            });

            // A top level expression that contains a Where condition.
            // It will track which paths (IJsonPathResolver) where evaluated,
            // which should be only the ones corresponding to true in resultsForWhereCondition.
            var mockExpression = new MockExpression(new ExpressionCommonProperties { ResourceType = resourceType, Path = path, Where = mockExpression_Where }, pathResolver =>
            {
                // The path at this index was evaluated
                int resultsIndex = mockPathResolvers_subLevel.IndexOf(pathResolver);
                actualEvaluations[resultsIndex] = true;
                numPathsEvaluated++;

                return new JsonRuleEvaluation(null, true, new JsonRuleResult[0]);
            });

            mockExpression.Evaluate(mockPathResolver_topLevel.Object);

            Assert.AreEqual(resultsForWhereCondition.Count(r => r), numPathsEvaluated);
            for (int i = 0; i < resultsForWhereCondition.Length; i++)
            {
                Assert.AreEqual(resultsForWhereCondition[i], actualEvaluations[i]);
            }
        }

        private List<IJsonPathResolver> GenerateMockPathResolvers(int count)
        {
            var resolvers = new List<IJsonPathResolver>();
            for (int i = 0; i < count; i++)
            {
                resolvers.Add(new Mock<IJsonPathResolver>().Object);
            }
            return resolvers;
        }
    }
}
