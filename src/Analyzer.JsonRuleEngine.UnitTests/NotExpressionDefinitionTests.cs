// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class NotExpressionDefinitionTests
    {
        [TestMethod]
        public void ToExpression_SingleLeafExpression_ReturnsNotExpressionAsExpected()
        {
            // Arrange
            var mockLeafExpressionDefinition = new Mock<LeafExpressionDefinition>();
            var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>().Object;
            mockLeafExpressionOperator.IsNegative = true;
            var mockLeafExpression = new Mock<LeafExpression>(mockLeafExpressionOperator, new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path" });
            mockLeafExpressionDefinition
                .Setup(s => s.ToExpression(true))
                .Returns(mockLeafExpression.Object);

            var notExpressionDefinition = new NotExpressionDefinition { Not = mockLeafExpressionDefinition.Object };

            // Act
            var notExpression = notExpressionDefinition.ToExpression() as NotExpression;

            // Assert
            mockLeafExpressionDefinition.Verify(s => s.ToExpression(true), Times.Once);
            Assert.AreEqual(notExpression.ExpressionToNegate, mockLeafExpression.Object);
            Assert.IsTrue(mockLeafExpression.Object.Operator.IsNegative);
        }

        [TestMethod]
        public void ToExpression_NotAllOfExpression_ReturnsNotExpressionAsExpected()
        {
            // Arrange
            var mockLeafExpressionDefinition = new Mock<LeafExpressionDefinition>();
            var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>().Object;
            var mockLeafExpression = new Mock<LeafExpression>(mockLeafExpressionOperator, new ExpressionCommonProperties { ResourceType = "ResourceProvider/resource", Path = "some.path" });
            mockLeafExpressionDefinition
                .Setup(s => s.ToExpression(true))
                .Returns(mockLeafExpression.Object);

            var mockAllOfExpressionDefinition = new Mock<AllOfExpressionDefinition>();
            mockAllOfExpressionDefinition.Object.AllOf =  new ExpressionDefinition[] { mockLeafExpressionDefinition.Object, mockLeafExpressionDefinition.Object };

            var mockAllOfExpression = new Mock<AllOfExpression>(new Expression[] { mockLeafExpression.Object, mockLeafExpression.Object }, new ExpressionCommonProperties());
            mockAllOfExpressionDefinition
                .Setup(s => s.ToExpression(true))
                .Returns(mockAllOfExpression.Object);

            var notExpressionDefinition = new NotExpressionDefinition { Not = mockAllOfExpressionDefinition.Object };

            // Act
            var notExpression = notExpressionDefinition.ToExpression() as NotExpression;

            // Assert
            mockAllOfExpressionDefinition.Verify(s => s.ToExpression(true), Times.Once);
            Assert.AreEqual(notExpression.ExpressionToNegate, mockAllOfExpression.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public void Validate_NullExpression_ThrowException()
        {
            var notExpressionDefinition = new NotExpressionDefinition();
            notExpressionDefinition.Validate();
        }
    }
}
