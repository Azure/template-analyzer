// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class EqualsOperatorTests
    {
        [DataTestMethod]
        [DataRow("value", DisplayName = "String values are equal")]
        [DataRow(true, DisplayName = "Boolean values are equal")]
        [DataRow(1, DisplayName = "Integer values are equal")]
        [DataRow(0.1, DisplayName = "Float values are equal")]
        [DataRow(new string[] { "value1", "value2" }, DisplayName = "Array values are equal")]
        [DataRow(@"{""property"": ""value""}", DisplayName = "Json values are equal")]
        [DataRow(2.0, 2, DisplayName = "Integer and float values are equal")]
        [DataRow("test", "Test", DisplayName = "Case-insensitive string values are equal")]
        public void EvaluateExpression_PropertyIsEqual_EqualsExpressionIsTrue_NotEqualsExpressionIsFalse(object expectedValue, object actualValue = null)
        {
            var expectedValueJToken = ToJToken(expectedValue);
            var actualValueJToken = ToJToken(actualValue) ?? expectedValueJToken;

            // {"Equals": jTokenValue} is true
            var equalsOperator = new EqualsOperator(expectedValueJToken, isNegative: false);
            Assert.IsTrue(equalsOperator.EvaluateExpression(actualValueJToken));

            // {"NotEquals": jTokenValue} is false
            var notEqualsOperator = new EqualsOperator(expectedValueJToken, isNegative: true);
            Assert.IsFalse(notEqualsOperator.EvaluateExpression(actualValueJToken));
        }

        [DataTestMethod]
        [DataRow("value", "value2", DisplayName = "String values are not equal")]
        [DataRow(true, false, DisplayName = "Boolean values are not equal")]
        [DataRow(1, 2, DisplayName = "Integer values are not equal")]
        [DataRow(0.1, 0.2, DisplayName = "Float values are not equal")]
        [DynamicData(nameof(TestArrays), DynamicDataSourceType.Method, DynamicDataDisplayName = "GetArrayValuesDynamicDataDisplayName")]
        [DataRow(@"{""property"": ""value11""}", @"{""property2"": ""value12""}", DisplayName = "Json values are not equal")]
        [DataRow("value", 2, DisplayName = "Values of different types are not equal")]
        [DataRow(2.3, 2, DisplayName = "Integer and float values are not equal")]
        public void EvaluateExpression_PropertyIsNotEqual_EqualsExpressionIsFalse_NotEqualsExpressionIsTrue(object expectedValue, object actualValue)
        {
            var expectedValueJToken = ToJToken(expectedValue);
            var actualValueJToken = ToJToken(actualValue);

            // {"Equals": jTokenValue} is false
            var equalsOperator = new EqualsOperator(expectedValueJToken, isNegative: false);
            Assert.IsFalse(equalsOperator.EvaluateExpression(actualValueJToken));

            // {"NotEquals": jTokenValue} is true
            var notEqualsOperator = new EqualsOperator(expectedValueJToken, isNegative: true);
            Assert.IsTrue(notEqualsOperator.EvaluateExpression(actualValueJToken));
        }

        public static string GetArrayValuesDynamicDataDisplayName(MethodInfo methodInfo, object[] data)
        {
            return "Array values are not equal";
        }

        static IEnumerable<object[]> TestArrays()
        {
            return new[] { new[] {
                new string[] { "value1", "value2" },
                new string[] { "value3", "value4" } }
            };
        }

        [TestMethod]
        public void GetName_ReturnsCorrectName()
        {
            Assert.AreEqual("Equals", new EqualsOperator(new JObject(), false).Name);
            Assert.AreEqual("NotEquals", new EqualsOperator(new JObject(), true).Name);
        }

        // Creates JSON with 'value' as the value of a key, parses it, then selects that key.
        private static JToken ToJToken(object value)
            => value == null ? null : JToken.Parse($"{{\"Key\": {JsonConvert.SerializeObject(value)} }}")["Key"];
    }
}
