// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
        public void EvaluateExpression(bool evaluationResult, params object[] arrayOfValues)
        {
            var aValue = "aValue";

            var valueJToken = ToJToken(aValue);
            var arrayOfValuesJToken = ToJToken(arrayOfValues);

            var inOperator = new InOperator(arrayOfValuesJToken, isNegative: false);
            Assert.AreEqual(evaluationResult, inOperator.EvaluateExpression(valueJToken));
        }

        // Creates JSON with 'value' as the value of a key, parses it, then selects that key.
        private static JToken ToJToken(object value)
            => JToken.Parse($"{{\"Key\": {JsonConvert.SerializeObject(value)} }}")["Key"];
    }
}