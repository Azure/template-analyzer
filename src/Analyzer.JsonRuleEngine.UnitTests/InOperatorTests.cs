// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class InOperatorTests
    {
        [DataTestMethod]
        [DataRow(true, 4, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Integer is in")]
        [DataRow(false, 8, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Integer is not in")]
        [DataRow(true, 3.5, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Float is in")]
        [DataRow(false, 9.5, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Float is not in")]
        [DataRow(true, "aValue", new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "String is in")]
        [DataRow(false, "someOtherValue", new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "String is not in")]
        [DataRow(true, false, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Boolean is in")]
        [DataRow(false, true, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Boolean is not in")]
        [DataRow(true, null, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Null is in")]
        [DataRow(false, null, new object[] { 4, 3.5, "aValue", false }, DisplayName = "Null is not in")]
        [DataRow(false, 7, new object[] { }, DisplayName = "Integer is not in empty array")]
        [DataRow(false, 7.7, new object[] { }, DisplayName = "Float is not in empty array")]
        [DataRow(false, "aValue", new object[] { }, DisplayName = "String is not in empty array")]
        [DataRow(false, true, new object[] { }, DisplayName = "Boolean is not in empty array")]
        [DataRow(false, null, new object[] { }, DisplayName = "Null is not in empty array")]
        public void EvaluateExpression_ValidDataType_ReturnsExpectedEvaluationResult(bool evaluationResult, object desiredValue, object arrayOfValues)
        {
            var desiredValueJToken = TestUtilities.ToJToken(desiredValue);
            var arrayOfValuesJToken = JArray.FromObject(arrayOfValues);

            var inOperator = new InOperator(arrayOfValuesJToken);

            Assert.AreEqual(evaluationResult, inOperator.EvaluateExpression(desiredValueJToken));
        }

        [DataTestMethod]
        [DataRow(false, 4, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Integer is in")]
        [DataRow(true, 8, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Integer is not in")]
        [DataRow(false, 3.5, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Float is in")]
        [DataRow(true, 9.5, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Float is not in")]
        [DataRow(false, "aValue", new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "String is in")]
        [DataRow(true, "someOtherValue", new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "String is not in")]
        [DataRow(false, false, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Boolean is in")]
        [DataRow(true, true, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Boolean is not in")]
        [DataRow(false, null, new object[] { 4, 3.5, "aValue", false, null }, DisplayName = "Null is in")]
        [DataRow(true, null, new object[] { 4, 3.5, "aValue", false }, DisplayName = "Null is not in")]
        [DataRow(true, 7, new object[] { }, DisplayName = "Integer is not in empty array")]
        [DataRow(true, 7.7, new object[] { }, DisplayName = "Float is not in empty array")]
        [DataRow(true, "aValue", new object[] { }, DisplayName = "String is not in empty array")]
        [DataRow(true, true, new object[] { }, DisplayName = "Boolean is not in empty array")]
        [DataRow(true, null, new object[] { }, DisplayName = "Null is not in empty array")]
        public void EvaluateExpression_ValidDataTypeIsNegative_ReturnsExpectedEvaluationResult(bool evaluationResult, object desiredValue, object arrayOfValues)
        {
            var desiredValueJToken = TestUtilities.ToJToken(desiredValue);
            var arrayOfValuesJToken = JArray.FromObject(arrayOfValues);

            var inOperator = new InOperator(arrayOfValuesJToken, true);

            Assert.AreEqual(evaluationResult, inOperator.EvaluateExpression(desiredValueJToken));
        }

        [TestMethod]
        public void EvaluateExpression_NullAsTokenToEvaluate_ReturnsFalse()
        {
            string[] possibleValues = { "aValue", "anotherValue" };
            var operatorValue = JArray.FromObject(possibleValues);
            var inOperator = new InOperator(operatorValue);

            Assert.IsFalse(inOperator.EvaluateExpression(null));
        }

        [TestMethod]
        public void GetName_ReturnsCorrectName()
        {
            Assert.AreEqual("In", new InOperator(new JArray()).Name);
        }
    }
}