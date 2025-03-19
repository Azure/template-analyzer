// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class HasValueOperatorTests
    {
        [TestMethod]
        [DataRow("value", DisplayName = "Property value is a string")]
        [DataRow(true, DisplayName = "Property value is a boolean")]
        [DataRow(1, DisplayName = "Property value is an integer")]
        [DataRow(0.1, DisplayName = "Property value is a float")]
        [DataRow((object)(new object[] { }), DisplayName = "Property value is an array")]
        public void EvaluateExpression_PropertyHasValue_HasValueIsTrue(object jTokenValue)
        {
            var jToken = TestUtilities.ToJToken(jTokenValue);

            // {"HasValue": true} is true
            var hasValueOperator = new HasValueOperator(true, isNegative: false);
            Assert.IsTrue(hasValueOperator.EvaluateExpression(jToken));

            // {"HasValue": false} is false
            hasValueOperator = new HasValueOperator(false, isNegative: false);
            Assert.IsFalse(hasValueOperator.EvaluateExpression(jToken));

            // Not {"HasValue": true} is false
            hasValueOperator = new HasValueOperator(true, isNegative: true);
            Assert.IsFalse(hasValueOperator.EvaluateExpression(jToken));

            // Not {"HasValue": false} is true
            hasValueOperator = new HasValueOperator(false, isNegative: true);
            Assert.IsTrue(hasValueOperator.EvaluateExpression(jToken));
        }

        [TestMethod]
        public void EvaluateExpression_PropertyIsObject_HasValueIsTrue()
        {
            var jToken = TestUtilities.ToJToken(new { });

            // {"HasValue": true} is true
            var hasValueOperator = new HasValueOperator(true, isNegative: false);
            Assert.IsTrue(hasValueOperator.EvaluateExpression(jToken));

            // {"HasValue": false} is false
            hasValueOperator = new HasValueOperator(false, isNegative: false);
            Assert.IsFalse(hasValueOperator.EvaluateExpression(jToken));
        }

        [TestMethod]
        [DataRow("", DisplayName = "Property value is an empty string")]
        [DataRow(null, DisplayName = "Property value is null")]
        public void EvaluateExpression_PropertyHasNullOrEmptyValue_HasValueIsFalse(object jTokenValue)
        {
            var jToken = TestUtilities.ToJToken(jTokenValue);

            // {"HasValue": true} is false
            var hasValueOperator = new HasValueOperator(true, isNegative: false);
            Assert.IsFalse(hasValueOperator.EvaluateExpression(jToken));

            // {"HasValue": false} is true
            hasValueOperator = new HasValueOperator(false, isNegative: false);
            Assert.IsTrue(hasValueOperator.EvaluateExpression(jToken));
        }

        [TestMethod]
        public void EvaluateExpression_PropertyDoesNotExist_HasValueIsFalse()
        {
            // {"HasValue": true} is false
            var hasValueOperator = new HasValueOperator(true, isNegative: false);
            Assert.IsFalse(hasValueOperator.EvaluateExpression(null));

            // {"HasValue": false} is true
            hasValueOperator = new HasValueOperator(false, isNegative: false);
            Assert.IsTrue(hasValueOperator.EvaluateExpression(null));
        }

        [TestMethod]
        public void GetName_ReturnsCorrectName()
        {
            Assert.AreEqual("HasValue", new HasValueOperator(true, false).Name);
        }
    }
}
