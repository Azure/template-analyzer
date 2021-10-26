// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class AnyOfExpressionDefinitionTests
    {
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
            var anyOfExpression = anyOfExpressionDefinition.ToExpression(false) as AnyOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(false), Times.Once);
            }

            Assert.AreEqual(numberOfExpressionDefinitions, anyOfExpression.AnyOf.Length);

            // Assert the expressions are equal to the mock objects
            for (int i = 0; i < anyOfExpression.AnyOf.Length; i++)
            {
                if (!(anyOfExpression.AnyOf[i] is LeafExpression leafExpression))
                {
                    continue;
                }

                Assert.AreEqual(mockLeafExpressionDefinitionsObject[i].ToExpression(), leafExpression);
            }
        }

        [DataTestMethod]
        [DataRow(1, DisplayName = "1 expression defined in AnyOf")]
        [DataRow(5, DisplayName = "5 expressions defined in AnyOf")]
        public void ToExpressionNegated_ValidExpressions_ReturnsArrayOfExpectedExpressions(int numberOfExpressionDefinitions)
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
            var allOfExpression = anyOfExpressionDefinition.ToExpression(true) as AllOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(true), Times.Once);
            }

            Assert.AreEqual(numberOfExpressionDefinitions, allOfExpression.AllOf.Length);

            // Assert the expressions are equal to the mock objects
            for (int i = 0; i < allOfExpression.AllOf.Length; i++)
            {
                if (!(allOfExpression.AllOf[i] is LeafExpression leafExpression))
                {
                    continue;
                }

                Assert.AreEqual(mockLeafExpressionDefinitionsObject[i].ToExpression(), leafExpression);
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
            var anyOfExpression = anyOfExpressionDefinition.ToExpression(false) as AnyOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(false), Times.Once);
            }

            singleMockLeafExpressionDefinition.Verify(s => s.ToExpression(false), Times.Once);

            Assert.AreEqual(2, anyOfExpression.AnyOf.Length);
            Assert.IsInstanceOfType(anyOfExpression.AnyOf.First(), typeof(AnyOfExpression));
            Assert.IsInstanceOfType(anyOfExpression.AnyOf.Last(), typeof(LeafExpression));
        }

        [TestMethod]
        public void ToExpressionNegated_NestedAnyOf_ReturnsArrayOfExpectedExpressions()
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
            var allOfExpression = anyOfExpressionDefinition.ToExpression(true) as AllOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(true), Times.Once);
            }

            singleMockLeafExpressionDefinition.Verify(s => s.ToExpression(true), Times.Once);

            Assert.AreEqual(2, allOfExpression.AllOf.Length);
            Assert.IsInstanceOfType(allOfExpression.AllOf.First(), typeof(AllOfExpression));
            Assert.IsInstanceOfType(allOfExpression.AllOf.Last(), typeof(LeafExpression));
        }

        private IEnumerable<Mock<LeafExpressionDefinition>> GenerateAnyOfExpressionDefinition(int numberOfExpressionDefinitions)
        {
            for (int i = 0; i < numberOfExpressionDefinitions; i++)
            {
                var mockLeafExpressionDefinition = new Mock<LeafExpressionDefinition>();
                var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>().Object;
                var mockLeafExpression = new Mock<LeafExpression>(mockLeafExpressionOperator, new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path" });
                mockLeafExpressionDefinition
                    .Setup(s => s.ToExpression(false))
                    .Returns(mockLeafExpression.Object);
                mockLeafExpressionDefinition
                    .Setup(s => s.ToExpression(true))
                    .Returns(mockLeafExpression.Object);

                yield return mockLeafExpressionDefinition;
            }
        }
    }
}
