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
    public class AllOfExpressionDefinitionTests
    {
        private ILineNumberResolver mockResolver = new Mock<ILineNumberResolver>().Object;

        [DataTestMethod]
        [DataRow(1, DisplayName = "1 expression defined in AllOf")]
        [DataRow(5, DisplayName = "5 expressions defined in AllOf")]
        public void ToExpression_ValidExpressions_ReturnsArrayOfExpectedExpressions(int numberOfExpressionDefinitions)
        {
            // Arrange
            var mockLeafExpressionDefinitions = GenerateAllOfExpressionDefinition(numberOfExpressionDefinitions).ToArray();
            var mockLeafExpressionDefinitionsObject = mockLeafExpressionDefinitions.Select(s => s.Object).ToArray();

            var allOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = mockLeafExpressionDefinitionsObject
            };

            // Act
            var allOfExpression = allOfExpressionDefinition.ToExpression(mockResolver, false) as AllOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver, false), Times.Once);
            }

            Assert.AreEqual(numberOfExpressionDefinitions, allOfExpression.AllOf.Length);

            // Assert the expressions are equal to the mock objects
            if (numberOfExpressionDefinitions > 0)
            {
                for (int i = 0; i < allOfExpression.AllOf.Length; i++)
                {
                    var leafExpression = allOfExpression.AllOf[i] as LeafExpression;

                    Assert.AreEqual(mockLeafExpressionDefinitionsObject[i].ToExpression(mockResolver, false), leafExpression);
                }
            }
        }

        [DataTestMethod]
        [DataRow(1, DisplayName = "1 expression defined in AllOf")]
        [DataRow(5, DisplayName = "5 expressions defined in AllOf")]
        public void ToExpressionNegated_ValidExpressions_ReturnsArrayOfExpectedExpressions(int numberOfExpressionDefinitions)
        {
            // Arrange
            var mockLeafExpressionDefinitions = GenerateAllOfExpressionDefinition(numberOfExpressionDefinitions).ToArray();
            var mockLeafExpressionDefinitionsObject = mockLeafExpressionDefinitions.Select(s => s.Object).ToArray();

            var allOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = mockLeafExpressionDefinitionsObject
            };

            // Act
            var anyOfExpression = allOfExpressionDefinition.ToExpression(mockResolver, true) as AnyOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver, true), Times.Once);
            }

            Assert.AreEqual(numberOfExpressionDefinitions, anyOfExpression.AnyOf.Length);

            // Assert the expressions are equal to the mock objects
            if (numberOfExpressionDefinitions > 0)
            {
                for (int i = 0; i < anyOfExpression.AnyOf.Length; i++)
                {
                    var leafExpression = anyOfExpression.AnyOf[i] as LeafExpression;

                    Assert.AreEqual(mockLeafExpressionDefinitionsObject[i].ToExpression(mockResolver, true), leafExpression);
                }
            }
        }

        [TestMethod]
        public void ToExpression_NestedAllOf_ReturnsArrayOfExpectedExpressions()
        {
            // Arrange
            var mockLeafExpressionDefinitions = GenerateAllOfExpressionDefinition(2).ToArray();
            var mockLeafExpressionDefinitionsObject = mockLeafExpressionDefinitions.Select(s => s.Object).ToArray();
            var singleMockLeafExpressionDefinition = GenerateAllOfExpressionDefinition(1).First();

            var nestedAllOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = mockLeafExpressionDefinitionsObject
            };

            var allOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = new ExpressionDefinition[] {nestedAllOfExpressionDefinition, singleMockLeafExpressionDefinition.Object }
            };

            // Act
            var allOfExpression = allOfExpressionDefinition.ToExpression(mockResolver, false) as AllOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver, false), Times.Once);
            }

            singleMockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver, false), Times.Once);

            Assert.AreEqual(2, allOfExpression.AllOf.Length);
            Assert.IsInstanceOfType(allOfExpression.AllOf.First(), typeof(AllOfExpression));
            Assert.IsInstanceOfType(allOfExpression.AllOf.Last(), typeof(LeafExpression));
        }

        [TestMethod]
        public void ToExpressionNegated_NestedAllOf_ReturnsArrayOfExpectedExpressions()
        {
            // Arrange
            var mockLeafExpressionDefinitions = GenerateAllOfExpressionDefinition(2).ToArray();
            var mockLeafExpressionDefinitionsObject = mockLeafExpressionDefinitions.Select(s => s.Object).ToArray();
            var singleMockLeafExpressionDefinition = GenerateAllOfExpressionDefinition(1).First();

            var nestedAllOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = mockLeafExpressionDefinitionsObject
            };

            var allOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = new ExpressionDefinition[] { nestedAllOfExpressionDefinition, singleMockLeafExpressionDefinition.Object }
            };

            // Act
            var anyOfExpression = allOfExpressionDefinition.ToExpression(mockResolver, true) as AnyOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver, true), Times.Once);
            }

            singleMockLeafExpressionDefinition.Verify(s => s.ToExpression(mockResolver, true), Times.Once);

            Assert.AreEqual(2, anyOfExpression.AnyOf.Length);
            Assert.IsInstanceOfType(anyOfExpression.AnyOf.First(), typeof(AnyOfExpression));
            Assert.IsInstanceOfType(anyOfExpression.AnyOf.Last(), typeof(LeafExpression));
        }

        private IEnumerable<Mock<LeafExpressionDefinition>> GenerateAllOfExpressionDefinition(int numberOfExpressionDefinitions)
        {
            for (int i = 0; i < numberOfExpressionDefinitions; i++)
            {
                var mockLeafExpressionDefinition = new Mock<LeafExpressionDefinition>();
                var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>().Object;
                var mockLineResolver = new Mock<ILineNumberResolver>().Object;
                var mockLeafExpression = new Mock<LeafExpression>(mockLineResolver, mockLeafExpressionOperator, new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path" });
                mockLeafExpressionDefinition
                    .Setup(s => s.ToExpression(mockResolver, false))
                    .Returns(mockLeafExpression.Object);
                mockLeafExpressionDefinition
                    .Setup(s => s.ToExpression(mockResolver, true))
                    .Returns(mockLeafExpression.Object);

                yield return mockLeafExpressionDefinition;
            }
        }
    }
}
