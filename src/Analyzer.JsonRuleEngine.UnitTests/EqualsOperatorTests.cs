// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class EqualsOperatorTests
    {
        [TestMethod]
        [DataRow("value", DisplayName = "String values are equal")]
        [DataRow(true, DisplayName = "Boolean values are equal")]
        [DataRow(1, DisplayName = "Integer values are equal")]
        [DataRow(0.1, DisplayName = "Float values are equal")]
        [DataRow(new string[] { "value1", "value2" }, DisplayName = "Array values are equal")]
        [DataRow(@"{""property"": ""value""}", DisplayName = "Json values are equal")]
        public void EvaluateExpression_PropertyIsEqual_EqualsExpressionIsTrue(object jTokenValue)
        {
            var jToken = ToJToken(jTokenValue);

            // {"Equals": jTokenValue} is true
            var equalsOperator = new EqualsOperator(jToken, isNegative: false);
            Assert.IsTrue(equalsOperator.EvaluateExpression(jToken));
        }

        [TestMethod]
        [DataRow("value", "value2", DisplayName = "String values are not equal")]
        [DataRow(true, false, DisplayName = "Boolean values are not equal")]
        [DataRow(1, 2, DisplayName = "Integer values are not equal")]
        [DataRow(0.1, 0.2, DisplayName = "Float values are not equal")]
        //[DataRow(new string[] { "value1", "value2" }, new string[] { "value3", "value4" }, DisplayName = "Array values are not equal")]
        [DataRow(@"{""property"": ""value""}", @"{""property2"": ""value2""}", DisplayName = "Json values are not equal")]
        public void EvaluateExpression_PropertyIsNotEqual_EqualsExpressionIsFalse(object expectedValue, object actualValue)
        {
            var expectedValueJToken = ToJToken(expectedValue);
            var actualValueJToken = ToJToken(actualValue);

            // {"Equals": jTokenValue} is false
            var equalsOperator = new EqualsOperator(expectedValueJToken, isNegative: false);
            Assert.IsFalse(equalsOperator.EvaluateExpression(actualValueJToken));
        }

        [TestMethod]
        public void GetName_ReturnsCorrectName()
        {
            Assert.AreEqual("Equals", new EqualsOperator(new JObject(), false).Name);
            Assert.AreEqual("NotEquals", new EqualsOperator(new JObject(), true).Name);
        }

        // Creates JSON with 'value' as the value of a key, parses it, then selects that key.
        private static JToken ToJToken(object value)
            => JToken.Parse($"{{\"Key\": {JsonConvert.SerializeObject(value)} }}")["Key"];
    }
}
