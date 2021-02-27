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
    public class AllOfExpressionDefinitionTests
    {
        [DataTestMethod]
        [DataRow(0, DisplayName = "0 expressions defined in AllOf - this error is caught in ExpressionConverter")]
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
            var allOfExpression = allOfExpressionDefinition.ToExpression() as AllOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(), Times.Once);
            }

            Assert.AreEqual(numberOfExpressionDefinitions, allOfExpression.AllOf.Length);

            // Assert the expressions are equal to the mock objects
            if (numberOfExpressionDefinitions > 0)
            {
                for (int i = 0; i < allOfExpression.AllOf.Length; i++)
                {
                    var leafExpression = allOfExpression.AllOf[i] as LeafExpression;

                    Assert.AreEqual(mockLeafExpressionDefinitionsObject[i].ToExpression(), leafExpression);
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
            var allOfExpression = allOfExpressionDefinition.ToExpression() as AllOfExpression;

            // Assert
            foreach (var mockLeafExpressionDefinition in mockLeafExpressionDefinitions)
            {
                mockLeafExpressionDefinition.Verify(s => s.ToExpression(), Times.Once);
            }

            singleMockLeafExpressionDefinition.Verify(s => s.ToExpression(), Times.Once);

            Assert.AreEqual(2, allOfExpression.AllOf.Length);
            Assert.IsInstanceOfType(allOfExpression.AllOf.First(), typeof(AllOfExpression));
            Assert.IsInstanceOfType(allOfExpression.AllOf.Last(), typeof(LeafExpression));
        }

        private IEnumerable<Mock<LeafExpressionDefinition>> GenerateAllOfExpressionDefinition(int numberOfExpressionDefinitions)
        {
            for (int i = 0; i < numberOfExpressionDefinitions; i++)
            {
                var mockLeafExpressionDefinition = new Mock<LeafExpressionDefinition>();
                var mockLeafExpressionOperator = new Mock<LeafExpressionOperator>().Object;
                var mockLeafExpression = new Mock<LeafExpression>(new object[] { "ResourceProvider/resource", "some.path", mockLeafExpressionOperator });
                mockLeafExpressionDefinition
                    .Setup(s => s.ToExpression())
                    .Returns(mockLeafExpression.Object);

                yield return mockLeafExpressionDefinition;
            }
        }
    }
}
