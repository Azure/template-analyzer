﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class RegexOperatorTests
    {
        [DataTestMethod]
        [DataRow("value", "aaavalueaaa", DisplayName = "String contains \"value\"")]
        [DataRow("value", "aaaVaLuEaaa", DisplayName = "String contains \"value\" - case-insensitive")]
        [DataRow("^value", "valueaaa", DisplayName = "String begins with \"value\"")]
        [DataRow("value$", "aaavalue", DisplayName = "String ends with \"value\"")]
        [DataRow("^((?!value).)*$", "aaa", DisplayName = "String does not contain \"value\"")]
        public void EvaluateExpression_StringMatchesRegex_EvaluationIsTrue(string regex, string stringToMatch)
        {
            // {"Regex": jTokenValue} is true
            var regexOperator = new RegexOperator(regex);
            Assert.IsTrue(regexOperator.EvaluateExpression(stringToMatch));

            // Not {"Regex": jTokenValue} is false
            var regexOperatorNegated = new RegexOperator(regex, true);
            Assert.IsFalse(regexOperatorNegated.EvaluateExpression(stringToMatch));
        }

        [DataTestMethod]
        [DataRow("value", "aaa", DisplayName = "String does not contain \"value\"")]
        [DataRow("^value", "aaavalue", DisplayName = "String does not begin with \"value\"")]
        [DataRow("value$", "valueaaa", DisplayName = "String does not end with \"value\"")]
        [DataRow("^((?!value).)*$", "value", DisplayName = "String contains \"value\"")]
        public void EvaluateExpression_StringDoesNotMatchRegex_EvaluationIsFalse(string regex, string stringToMatch)
        {
            // {"Regex": jTokenValue} is false
            var regexOperator = new RegexOperator(regex);
            Assert.IsFalse(regexOperator.EvaluateExpression(stringToMatch));

            // Not {"Regex": jTokenValue} is true
            var regexOperatorNegated = new RegexOperator(regex, true);
            Assert.IsTrue(regexOperatorNegated.EvaluateExpression(stringToMatch));
        }

        [DataTestMethod]
        [DataRow(0, DisplayName = "Property is an int")]
        [DataRow(false, DisplayName = "Property is a boolean")]
        [DataRow(0.1, DisplayName = "Property is a float")]
        [DataRow(new string[] { "value" }, DisplayName = "Property is an array")]
        [DataRow(null, DisplayName = "Property is null")]
        [DataRow((string)null, DisplayName = "Property is a null casted as string")]
        public void EvaluateExpression_PropertyIsNotString_EvaluationIsFalse(object objectToMatch)
        {
            var regexOperator = new RegexOperator("");
            Assert.IsFalse(regexOperator.EvaluateExpression(TestUtilities.ToJToken(objectToMatch)));
        }

        [TestMethod]
        public void EvaluateExpression_PropertyIsMissing_EvaluationIsFalse()
        {
            var regexOperator = new RegexOperator("");
            Assert.IsFalse(regexOperator.EvaluateExpression(null));
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void EvaluateExpresssion_RegexPatternInvalid_ArgumentExceptionIsThrown()
        {
            new RegexOperator("[");
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentNullException))]
        public void EvaluateExpresssion_RegexPatternNull_ArgumentNullExceptionIsThrown()
        {
            new RegexOperator(null);
        }

        [TestMethod]
        public void GetName_ReturnsCorrectName()
        {
            Assert.AreEqual("Regex", new RegexOperator("").Name);
        }
    }
}
