// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class InequalityOperatorTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullSpecifiedValue_ThrowsException()
        {
            new InequalityOperator(null, true, true);
        }

        [TestMethod]
        public void Constructor_InvalidSpecifiedValueType_ThrowsException()
        {
            var specifiedValueToken = TestUtilities.ToJToken("aString");

            ValidateInvalidOperationException(() => { new InequalityOperator(specifiedValueToken, true, true); }, "Cannot compare against a String using an InequalityOperator");
        }

        [TestMethod]
        public void GetName_ReturnsCorrectName()
        {
            var specifiedValue = TestUtilities.ToJToken(100);

            Assert.AreEqual("LessOrEquals", new InequalityOperator(specifiedValue, isNegative: true, orEquals: true).Name);
            Assert.AreEqual("Less", new InequalityOperator(specifiedValue, isNegative: true, orEquals: false).Name);
            Assert.AreEqual("GreaterOrEquals", new InequalityOperator(specifiedValue, isNegative: false, orEquals: true).Name);
            Assert.AreEqual("Greater", new InequalityOperator(specifiedValue, isNegative: false, orEquals: false).Name);

        }

        [DataTestMethod]
        // >int
        [DataRow(2, 1, false, false, true, DisplayName = "An integer is greater than another integer")]
        [DataRow(1, 2, false, false, false, DisplayName = "An integer is not greater than another integer")]
        // <int
        [DataRow(1, 2, true, false, true, DisplayName = "An integer is less than another integer")]
        [DataRow(2, 1, true, false, false, DisplayName = "An integer is not less than another integer")]
        // >=int
        [DataRow(2, 1, false, true, true, DisplayName = "An integer is greater or equal to another integer")]
        [DataRow(1, 2, false, true, false, DisplayName = "An integer is not greater or equal to another integer")]
        [DataRow(2, 2, false, true, true, DisplayName = "An integer is greater or equal to another integer because both are equal")]
        // <=int
        [DataRow(1, 2, true, true, true, DisplayName = "An integer is less or equal to another integer")]
        [DataRow(2, 1, true, true, false, DisplayName = "An integer is not less or equal to another integer")]
        [DataRow(1, 1, true, true, true, DisplayName = "An integer is less or equal to another integer because both are equal")]
        // >float
        [DataRow(2.8, 1.3, false, false, true, DisplayName = "A float is greater than another float")]
        [DataRow(1.3, 2.8, false, false, false, DisplayName = "A float is not greater than another float")]
        // <float
        [DataRow(1.3, 2.8, true, false, true, DisplayName = "A float is less than another float")]
        [DataRow(2.8, 1.3, true, false, false, DisplayName = "A float is not less than another float")]
        // >=float
        [DataRow(2.8, 1.3, false, true, true, DisplayName = "A float is greater or equal to another float")]
        [DataRow(1.3, 2.8, false, true, false, DisplayName = "A float is not greater or equal to another float")]
        [DataRow(2.8, 2.8, false, true, true, DisplayName = "A float is greater or equal to another float because both are equal")]
        // <=float
        [DataRow(1.3, 2.8, true, true, true, DisplayName = "A float is less or equal to another float")]
        [DataRow(2.8, 1.3, true, true, false, DisplayName = "A float is not less or equal to another float")]
        [DataRow(1.3, 1.3, true, true, true, DisplayName = "A float is less or equal to another float because both are equal")]
        public void EvaluateExpression_ValidNumericType_ReturnsExpectedEvaluationResult(object leftValue, object rightValue, bool isNegative, bool orEquals, bool evaluationResult)
        {
            CompareObjects(leftValue, rightValue, isNegative, orEquals, evaluationResult);
        }

        [DataTestMethod]
        // >date
        [DataRow(637676928000000000, 637500672000000000, false, false, true, DisplayName = "A date is greater than another date")]
        [DataRow(637500672000000000, 637676928000000000, false, false, false, DisplayName = "A date is not greater than another date")]
        // <date
        [DataRow(637500672000000000, 637676928000000000, true, false, true, DisplayName = "A date is less than another date")]
        [DataRow(637676928000000000, 637500672000000000, true, false, false, DisplayName = "A date is not less than another date")]
        // >=date
        [DataRow(637676928000000000, 637500672000000000, false, true, true, DisplayName = "A date is greater or equal to another date")]
        [DataRow(637500672000000000, 637676928000000000, false, true, false, DisplayName = "A date is not greater or equal to another date")]
        [DataRow(637676928000000000, 637676928000000000, false, true, true, DisplayName = "A date is greater or equal to another date because both are equal")]
        // <=date
        [DataRow(637500672000000000, 637676928000000000, true, true, true, DisplayName = "A date is less or equal to another date")]
        [DataRow(637676928000000000, 637500672000000000, true, true, false, DisplayName = "A date is not less or equal to another date")]
        [DataRow(637500672000000000, 637500672000000000, true, true, true, DisplayName = "A date is less or equal to another date because both are equal")]
        public void EvaluateExpression_ValidDateType_ReturnsExpectedEvaluationResult(long leftTicks, long rightTicks, bool isNegative, bool orEquals, bool evaluationResult)
        {
            var leftDate = new DateTime(leftTicks);
            var rightDate = new DateTime(rightTicks);

            CompareObjects(leftDate, rightDate, isNegative, orEquals, evaluationResult);
        }

        [TestMethod]
        public void EvaluateExpression_InvalidTokenToEvaluate_ThrowsException()
        {
            var specifiedValueToken = TestUtilities.ToJToken(100);
            var tokenToEvaluate = TestUtilities.ToJToken("aString");

            var inequalityOperator = new InequalityOperator(specifiedValueToken, true, true);

            ValidateInvalidOperationException(() => { inequalityOperator.EvaluateExpression(tokenToEvaluate); }, "Cannot compare against a String using an InequalityOperator");
        }

        [TestMethod]
        public void EvaluateExpression_CompareDateWithNumber_ThrowsException()
        {
            var date = new DateTime(637500672000000000);
            var number = 100;

            ValidateInvalidOperationException(() => { CompareObjects(date, number); }, "Cannot compare Date with Integer using an InequalityOperator");
        }

        [TestMethod]
        public void EvaluateExpression_CompareNumberWithDate_ThrowsException()
        {
            var number = 100;
            var date = new DateTime(637500672000000000);

            ValidateInvalidOperationException(() => { CompareObjects(number, date); }, "Cannot compare Integer with Date using an InequalityOperator");
        }

        private void CompareObjects(object left, object right, bool isNegative = false, bool orEquals = false, bool evaluationResult = false)
        {
            var leftToken = TestUtilities.ToJToken(left);
            var rightToken = TestUtilities.ToJToken(right);

            var inequalityOperator = new InequalityOperator(leftToken, isNegative: isNegative, orEquals: orEquals);

            Assert.AreEqual(evaluationResult, inequalityOperator.EvaluateExpression(rightToken));

            Assert.AreEqual(false, inequalityOperator.EvaluateExpression(null));
        }
        
        private void ValidateInvalidOperationException(Action funct, string exceptionMessage)
        {
            try
            {
                funct();
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(exceptionMessage, ex.Message);

                return;
            }

            Assert.IsTrue(false, "TestMethod should have thrown an exception");
        }
    }
}