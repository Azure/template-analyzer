// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.JsonRuleEngine.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class AllOfExpressionDefinitionTests
    {
        [DataTestMethod]
        [DataRow(0, DisplayName = "0 expressions defined in AllOf")]
        [DataRow(1, DisplayName = "1 expression defined in AllOf")]
        [DataRow(5, DisplayName = "5 expressions defined in AllOf")]
        public void ToExpression_ValidExpressions_ReturnsArrayOfExpectedExpressions(int numberOfExpressionDefinitions)
        {
            // Arrange
            var mockLeafExpressionDefinition = new Mock<LeafExpressionDefinition>();
            var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>().Object;
            var mockLeafExpression = new Mock<LeafExpression>(new object[] { "ResourceProvider/resource", "some.path",  mockLeafExpressionOperator});
            mockLeafExpressionDefinition
                .Setup(s => s.ToExpression())
                .Returns(mockLeafExpression.Object);

            var allOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = GenerateAllOfExpressionDefinition(numberOfExpressionDefinitions, mockLeafExpressionDefinition.Object).ToArray()
            };

            // Act
            var allOfExpression = allOfExpressionDefinition.ToExpression() as AllOfExpression;

            // Assert
            mockLeafExpressionDefinition.Verify(s => s.ToExpression(), Times.Exactly(numberOfExpressionDefinitions));

            Assert.AreEqual(numberOfExpressionDefinitions, allOfExpression.AllOf.Length);
            if (numberOfExpressionDefinitions > 0)
            {
                var firstExpression = allOfExpression.AllOf.First() as LeafExpression;
                Assert.AreEqual("some.path", firstExpression.Path);
            }
        }

        [TestMethod]
        public void ToExpression_NestedAllOf_ReturnsArrayOfExpectedExpressions()
        {
            // Arrange
            var mockLeafExpressionDefinition = new Mock<LeafExpressionDefinition>();
            var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>().Object;
            var mockLeafExpression = new Mock<LeafExpression>(new object[] { "ResourceProvider/resource", "some.path", mockLeafExpressionOperator });
            mockLeafExpressionDefinition
                .Setup(s => s.ToExpression())
                .Returns(mockLeafExpression.Object);

            var nestedAllOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = GenerateAllOfExpressionDefinition(2, mockLeafExpressionDefinition.Object).ToArray()
            };

            var allOfExpressionDefinition = new AllOfExpressionDefinition
            {
                Path = "json.path",
                AllOf = new ExpressionDefinition[] {nestedAllOfExpressionDefinition, mockLeafExpressionDefinition.Object }
            };

            // Act
            var allOfExpression = allOfExpressionDefinition.ToExpression() as AllOfExpression;

            // Assert
            mockLeafExpressionDefinition.Verify(s => s.ToExpression(), Times.Exactly(3));

            Assert.AreEqual(2, allOfExpression.AllOf.Length);
            Assert.IsInstanceOfType(allOfExpression.AllOf.First(), typeof(AllOfExpression));
            Assert.IsInstanceOfType(allOfExpression.AllOf.Last(), typeof(LeafExpression));
        }

        private IEnumerable<ExpressionDefinition> GenerateAllOfExpressionDefinition(int numberOfExpressionDefinitions, LeafExpressionDefinition leafExpressionDefinition)
        {
            for (int i = 0; i < numberOfExpressionDefinitions; i++)
            {
                yield return leafExpressionDefinition;
            }
        }
    }
}
