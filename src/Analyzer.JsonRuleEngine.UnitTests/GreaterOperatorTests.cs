// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class GreaterOperatorTests
    {
        [DataTestMethod]
        [DataRow(2, 1, false, true, DisplayName = "An integer is greater than another integer")]
        [DataRow(1, 2, false, false, DisplayName = "An integer is not greater than another integer")]
        [DataRow(1, 2, true, true, DisplayName = "An integer is less than another integer")]
        [DataRow(2, 1, true, false, DisplayName = "An integer is not less than another integer")]
        [DataRow(2.8, 1.3, false, true, DisplayName = "A float is greater than another float")]
        [DataRow(1.3, 2.8, false, false, DisplayName = "A float is not greater than another float")]
        [DataRow(1.3, 2.8, true, true, DisplayName = "A float is less than another float")]
        [DataRow(2.8, 1.3, true, false, DisplayName = "A float is not less than another float")]

        public void EvaluateExpression_ValidNumericType_ReturnsExpectedEvaluationResult(object leftValue, object rightValue, bool isNegative, bool evaluationResult) // TODO double check right/left
        {
            CompareObjects(leftValue, rightValue, isNegative, evaluationResult);
        }

        [DataTestMethod]
        [DataRow(637676928000000000, 637500672000000000, false, true, DisplayName = "A date is greater than another date")]
        [DataRow(637500672000000000, 637676928000000000, false, false, DisplayName = "A date is not greater than another date")]
        [DataRow(637500672000000000, 637676928000000000, true, true, DisplayName = "A date is less than another date")]
        [DataRow(637676928000000000, 637500672000000000, true, false, DisplayName = "A date is not less than another date")]
        public void EvaluateExpression_ValidDateType_ReturnsExpectedEvaluationResult(long leftTicks, long rightTicks, bool isNegative, bool evaluationResult)
        {
            var leftDate = new DateTime(leftTicks);
            var rightDate = new DateTime(rightTicks);

            CompareObjects(leftDate, rightDate, isNegative, evaluationResult);
        }

        [TestMethod]
        public void GetName_ReturnsCorrectName()
        {
            Assert.AreEqual("Greater", new GreaterOperator(new JObject(), false).Name);
            Assert.AreEqual("Less", new GreaterOperator(new JObject(), true).Name);
        }

        private void CompareObjects(object left, object right, bool isNegative, bool evaluationResult)
        {
            var leftJToken = TestUtilities.ToJToken(left);
            var rightJToken = TestUtilities.ToJToken(right);

            var greaterOperator = new GreaterOperator(leftJToken, isNegative: isNegative);

            Assert.AreEqual(evaluationResult, greaterOperator.EvaluateExpression(rightJToken));
        }
    }
}