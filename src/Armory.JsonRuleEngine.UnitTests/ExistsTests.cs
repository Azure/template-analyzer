﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class ExistsTests
    {
        [TestMethod]
        [DataRow("value", DisplayName = "Property value is a non-empty string")]
        [DataRow("", DisplayName = "Property value is an empty string")]
        [DataRow(true, DisplayName = "Property value is a boolean")]
        [DataRow(1, DisplayName = "Property value is an integer")]
        [DataRow(0.1, DisplayName = "Property value is a float")]
        [DataRow(new object[] { }, DisplayName = "Property value is an object")]
        [DataRow(null, DisplayName = "Property value is null")]
        public void EvaluateExpression_PropertyExists_ExistsExpressionIsTrue(object jTokenValue)
        {
            var jToken = ToJToken(jTokenValue);

            // {"Exists": true} is true
            var existsOperator = new ExistsOperator(true, isNegative: false);
            Assert.IsTrue(existsOperator.EvaluateExpression(jToken));

            // {"Exists": false} is false
            existsOperator = new ExistsOperator(false, isNegative: false);
            Assert.IsFalse(existsOperator.EvaluateExpression(jToken));
        }

        [TestMethod]
        public void EvaluateExpression_PropertyDoesNotExist_ExistsExpressionIsFalse()
        {
            // {"Exists": true} is false for null JToken
            var existsOperator = new ExistsOperator(true, isNegative: false);
            Assert.IsFalse(existsOperator.EvaluateExpression(null));

            // {"Exists": false} is true for null JToken
            existsOperator = new ExistsOperator(false, isNegative: false);
            Assert.IsTrue(existsOperator.EvaluateExpression(null));
        }

        // Creates JSON with 'value' as the value of a key, parses it, then selects that key.
        private static JToken ToJToken(object value)
            => JToken.Parse($"{{\"Key\": {JsonConvert.SerializeObject(value)} }}")["Key"];
    }
}