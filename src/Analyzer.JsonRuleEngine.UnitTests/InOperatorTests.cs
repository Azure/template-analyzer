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
        [DataRow(true, new object[] { "anotherValue", "aValue" }, DisplayName = "String is in string array")]
        [DataRow(false, new object[] { "anotherValue", "otherValue" }, DisplayName = "String is not in string array")]
        [DataRow(false, new object[] { }, DisplayName = "String is not in empty array")]
        [DataRow(true, new object[] { "anotherValue", 4, false, 3.5, null, "aValue" }, DisplayName = "String is in mixed array")]
        [DataRow(false, new object[] { "anotherValue", 4, false, 3.5, null, "otherValue" }, DisplayName = "String is not in mixed array")]
        public void EvaluateExpression(bool evaluationResult, params object[] arrayOfValues)
        {
            var aValue = "aValue";

            var valueJToken = JsonRuleEngineTestsUtilities.ToJToken(aValue);
            var arrayOfValuesJToken = JsonRuleEngineTestsUtilities.ToJToken(arrayOfValues);

            var inOperator = new InOperator(arrayOfValuesJToken, isNegative: false);
            Assert.AreEqual(evaluationResult, inOperator.EvaluateExpression(valueJToken));
        }

        [TestMethod]
        public void GetName_ReturnsCorrectName()
        {
            Assert.AreEqual("In", new InOperator(new JObject(), false).Name);
        }
    }
}