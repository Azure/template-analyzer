// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class AnyOfExpressionDefinitionTests
    {
        private ILineNumberResolver mockResolver = new Mock<ILineNumberResolver>().Object;

        [DataTestMethod]
        [DataRow(1, DisplayName = "1 expression defined in AnyOf")]
        [DataRow(5, DisplayName = "5 expressions defined in AnyOf")]
        public void ToExpression_ValidExpressions_ReturnsArrayOfExpectedExpressions(int numberOfExpressionDefinitions)
        {
            // Arrange
            var mockLeafExpressionDefinitions = GenerateAnyOfExpressionDefinition(numberOfExpressionDefinitions).ToArray();
            var mockLeafExpressionDefinitionsObject = mockLeafExpressionDefinitions.Select(s => s.Object).ToList();

            AnyOfExpressionDefinition anyOfExpressionDefinition = new AnyOfExpressionDefinition
            {
                Path = "json.path",
                AnyOf = mockLeafExpressionDefinitionsObject.ToArray()
            };

            // Act
            var anyOfExpression = anyOfExpressionDefinition.ToExpression(mockResolver) as AnyOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver), Times.Once);
            }

            Assert.AreEqual(numberOfExpressionDefinitions, anyOfExpression.AnyOf.Length);

            // Assert the expressions are equal to the mock objects
            for (int i = 0; i < anyOfExpression.AnyOf.Length; i++)
            {
                if (!(anyOfExpression.AnyOf[i] is LeafExpression leafExpression))
                {
                    continue;
                }

                Assert.AreEqual(mockLeafExpressionDefinitionsObject[i].ToExpression(mockResolver), leafExpression);
            }
        }

        [TestMethod]
        public void ToExpression_NestedAnyOf_ReturnsArrayOfExpectedExpressions()
        {
            // Arrange
            var mockLeafExpressionDefinitions = GenerateAnyOfExpressionDefinition(2).ToArray();
            var mockLeafExpressionDefinitionsObject = mockLeafExpressionDefinitions.Select(s => s.Object).ToArray();
            var singleMockLeafExpressionDefinition = GenerateAnyOfExpressionDefinition(1).First();

            var nestedAnyOfExpressionDefinition = new AnyOfExpressionDefinition
            {
                Path = "json.path",
                AnyOf = mockLeafExpressionDefinitionsObject
            };

            var anyOfExpressionDefinition = new AnyOfExpressionDefinition
            {
                Path = "json.path",
                AnyOf = new ExpressionDefinition[] { nestedAnyOfExpressionDefinition, singleMockLeafExpressionDefinition.Object }
            };

            // Act
            var anyOfExpression = anyOfExpressionDefinition.ToExpression(mockResolver) as AnyOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver), Times.Once);
            }

            singleMockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver), Times.Once);

            Assert.AreEqual(2, anyOfExpression.AnyOf.Length);
            Assert.IsInstanceOfType(anyOfExpression.AnyOf.First(), typeof(AnyOfExpression));
            Assert.IsInstanceOfType(anyOfExpression.AnyOf.Last(), typeof(LeafExpression));
        }

        private IEnumerable<Mock<LeafExpressionDefinition>> GenerateAnyOfExpressionDefinition(int numberOfExpressionDefinitions)
        {
            for (int i = 0; i < numberOfExpressionDefinitions; i++)
            {
                var mockLeafExpressionDefinition = new Mock<LeafExpressionDefinition>();
                var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>().Object;
                var mockLineResolver = new Mock<ILineNumberResolver>().Object;
                var mockLeafExpression = new Mock<LeafExpression>(mockLineResolver, mockLeafExpressionOperator, "ResourceProvider/resource", "some.path");
                mockLeafExpressionDefinition
                    .Setup(s => s.ToExpression(mockResolver))
                    .Returns(mockLeafExpression.Object);

                yield return mockLeafExpressionDefinition;
            }
        }
    }
}
