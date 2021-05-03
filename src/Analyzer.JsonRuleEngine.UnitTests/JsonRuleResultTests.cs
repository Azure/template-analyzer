// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Constants;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class JsonRuleResultTests
    {
        [DataTestMethod]
        [DataRow("EXPECTED_VALUE", "ACTUAL_VALUE", DisplayName = "JToken values are strings")]
        [DataRow(1, 2, DisplayName = "JToken values are ints")]
        [DataRow(true, false, DisplayName = "JToken values are bools")]
        [DataRow(null, "ACTUAL_VALUE", DisplayName = "Expected value is null")]
        [DataRow("EXPECTED_VALUE", null, DisplayName = "Actual value is null")]
        public void FailureMessage_ValidJTokensForEqualsOperator_FailureMessageIsReturnedAsExpected(object expectedValue, object actualValue)
        {
            // Arrange
            var mockOperator = new Mock<LeafExpressionOperator>();
            mockOperator
                .Setup(s => s.EvaluateExpression(It.IsAny<JToken>()))
                .Returns(false);
            mockOperator.Object.SpecifiedValue = TestUtilities.ToJToken(expectedValue);
            mockOperator.Object.FailureMessage = JsonRuleEngineConstants.EqualsFailureMessage;

            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            mockJsonPathResolver
                .Setup(s => s.JToken)
                .Returns(TestUtilities.ToJToken(actualValue));
            mockJsonPathResolver
                .Setup(s => s.Path)
                .Returns("some.path");

            var mockLineNumberResolver = new Mock<ILineNumberResolver>();
            mockLineNumberResolver
                .Setup(s => s.ResolveLineNumber(mockJsonPathResolver.Object.Path))
                .Returns(0);

            var mockExpression = new LeafExpression(mockLineNumberResolver.Object, mockOperator.Object, new ExpressionCommonProperties { Path = "some.path" });

            var result = new JsonRuleResult()
            {
                Passed = mockOperator.Object.EvaluateExpression(mockJsonPathResolver.Object.JToken),
                JsonPath = mockJsonPathResolver.Object.Path,
                LineNumber = mockLineNumberResolver.Object.ResolveLineNumber(mockJsonPathResolver.Object.Path),
                Expression = mockExpression,
                ActualValue = mockJsonPathResolver.Object.JToken
            };

            // Act
            string failureMessage = result.FailureMessage();

            // Assert
            Assert.AreEqual($"Value \"{actualValue ?? "null"}\" found at \"some.path\" is not equal to \"{expectedValue ?? "null"}\".", failureMessage);
        }

        [DataTestMethod]
        [DataRow(true, false, DisplayName = "JToken at path does not exist")]
        [DataRow(false, true, DisplayName = "JToken at path exists")]
        [DataRow(true, null, DisplayName = "JToken at path is null")]
        public void FailureMessage_ValidJTokensForExistsOperator_FailureMessageIsReturnedAsExpected(object expectedValue, object actualValue)
        {
            // Arrange
            var mockOperator = new Mock<LeafExpressionOperator>();
            mockOperator
                .Setup(s => s.EvaluateExpression(It.IsAny<JToken>()))
                .Returns(false);
            mockOperator.Object.SpecifiedValue = TestUtilities.ToJToken(expectedValue);
            mockOperator.Object.FailureMessage = JsonRuleEngineConstants.ExistsFailureMessage;

            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            mockJsonPathResolver
                .Setup(s => s.JToken)
                .Returns(TestUtilities.ToJToken(actualValue));
            mockJsonPathResolver
                .Setup(s => s.Path)
                .Returns("some.path");

            var mockLineNumberResolver = new Mock<ILineNumberResolver>();
            mockLineNumberResolver
                .Setup(s => s.ResolveLineNumber(mockJsonPathResolver.Object.Path))
                .Returns(0);

            var mockExpression = new LeafExpression(mockLineNumberResolver.Object, mockOperator.Object, new ExpressionCommonProperties { Path = "some.path" });

            var result = new JsonRuleResult()
            {
                Passed = mockOperator.Object.EvaluateExpression(mockJsonPathResolver.Object.JToken),
                JsonPath = mockJsonPathResolver.Object.Path,
                LineNumber = mockLineNumberResolver.Object.ResolveLineNumber(mockJsonPathResolver.Object.Path),
                Expression = mockExpression,
                ActualValue = mockJsonPathResolver.Object.JToken
            };

            // Act
            string failureMessage = result.FailureMessage();

            // Assert
            Assert.AreEqual($"Value found at \"some.path\" exists: {actualValue ?? "null"}, expected: {expectedValue}.", failureMessage);
        }

        [DataTestMethod]
        [DataRow(true, false, DisplayName = "JToken at path does not have a value")]
        [DataRow(false, true, DisplayName = "JToken at path has a value")]
        [DataRow(true, null, DisplayName = "JToken at path is null")]
        public void FailureMessage_ValidJTokensForHasValueOperator_FailureMessageIsReturnedAsExpected(object expectedValue, object actualValue)
        {
            // Arrange
            var mockOperator = new Mock<LeafExpressionOperator>();
            mockOperator
                .Setup(s => s.EvaluateExpression(It.IsAny<JToken>()))
                .Returns(false);
            mockOperator.Object.SpecifiedValue = TestUtilities.ToJToken(expectedValue);
            mockOperator.Object.FailureMessage = JsonRuleEngineConstants.HasValueFailureMessage;

            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            mockJsonPathResolver
                .Setup(s => s.JToken)
                .Returns(TestUtilities.ToJToken(actualValue));
            mockJsonPathResolver
                .Setup(s => s.Path)
                .Returns("some.path");

            var mockLineNumberResolver = new Mock<ILineNumberResolver>();
            mockLineNumberResolver
                .Setup(s => s.ResolveLineNumber(mockJsonPathResolver.Object.Path))
                .Returns(0);

            var mockExpression = new LeafExpression(mockLineNumberResolver.Object, mockOperator.Object, new ExpressionCommonProperties { Path = "some.path" });

            var result = new JsonRuleResult()
            {
                Passed = mockOperator.Object.EvaluateExpression(mockJsonPathResolver.Object.JToken),
                JsonPath = mockJsonPathResolver.Object.Path,
                LineNumber = mockLineNumberResolver.Object.ResolveLineNumber(mockJsonPathResolver.Object.Path),
                Expression = mockExpression,
                ActualValue = mockJsonPathResolver.Object.JToken
            };

            // Act
            string failureMessage = result.FailureMessage();

            // Assert
            Assert.AreEqual($"Value found at \"some.path\" has a value: {actualValue ?? "null"}, expected: {expectedValue}.", failureMessage);
        }

        [DataTestMethod]
        [DataRow("value", "doesNotContain", DisplayName = "JToken at path does not match regex pattern")]
        [DataRow("value", null, DisplayName = "JToken at path is null")]
        public void FailureMessage_ValidJTokensForRegexOperator_FailureMessageIsReturnedAsExpected(object expectedValue, object actualValue)
        {
            // Arrange
            var mockOperator = new Mock<LeafExpressionOperator>();
            mockOperator
                .Setup(s => s.EvaluateExpression(It.IsAny<JToken>()))
                .Returns(false);
            mockOperator.Object.SpecifiedValue = TestUtilities.ToJToken(expectedValue);
            mockOperator.Object.FailureMessage = JsonRuleEngineConstants.RegexFailureMessage;

            var mockJsonPathResolver = new Mock<IJsonPathResolver>();
            mockJsonPathResolver
                .Setup(s => s.JToken)
                .Returns(TestUtilities.ToJToken(actualValue));
            mockJsonPathResolver
                .Setup(s => s.Path)
                .Returns("some.path");

            var mockLineNumberResolver = new Mock<ILineNumberResolver>();
            mockLineNumberResolver
                .Setup(s => s.ResolveLineNumber(mockJsonPathResolver.Object.Path))
                .Returns(0);

            var mockExpression = new LeafExpression(mockLineNumberResolver.Object, mockOperator.Object, new ExpressionCommonProperties { Path = "some.path" });

            var result = new JsonRuleResult()
            {
                Passed = mockOperator.Object.EvaluateExpression(mockJsonPathResolver.Object.JToken),
                JsonPath = mockJsonPathResolver.Object.Path,
                LineNumber = mockLineNumberResolver.Object.ResolveLineNumber(mockJsonPathResolver.Object.Path),
                Expression = mockExpression,
                ActualValue = mockJsonPathResolver.Object.JToken
            };

            // Act
            string failureMessage = result.FailureMessage();

            // Assert
            Assert.AreEqual($"Value \"{actualValue ?? "null"}\" found at \"some.path\" does not match regex pattern: \"{expectedValue}\".", failureMessage);
        }
    }
}
