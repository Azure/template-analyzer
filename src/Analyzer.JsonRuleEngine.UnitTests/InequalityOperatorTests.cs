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

            Assert.AreEqual("LessOrEquals", new InequalityOperator(specifiedValue, greater: false, orEquals: true).Name);
            Assert.AreEqual("Less", new InequalityOperator(specifiedValue, greater: false, orEquals: false).Name);
            Assert.AreEqual("GreaterOrEquals", new InequalityOperator(specifiedValue, greater: true, orEquals: true).Name);
            Assert.AreEqual("Greater", new InequalityOperator(specifiedValue, greater: true, orEquals: false).Name);
        }

        [DataTestMethod]
        // >int
        [DataRow(2, 1, true, false, true, DisplayName = "An integer is greater than another integer")]
        [DataRow(1, 2, true, false, false, DisplayName = "An integer is not greater than another integer")]
        [DataRow(1, 1, true, false, false, DisplayName = "An integer is not greater than another integer because both are equal")]
        // <int
        [DataRow(1, 2, false, false, true, DisplayName = "An integer is less than another integer")]
        [DataRow(2, 1, false, false, false, DisplayName = "An integer is not less than another integer")]
        [DataRow(1, 1, false, false, false, DisplayName = "An integer is not less than another integer because both are equal")]
        // >=int
        [DataRow(2, 1, true, true, true, DisplayName = "An integer is greater or equal to another integer")]
        [DataRow(1, 2, true, true, false, DisplayName = "An integer is not greater or equal to another integer")]
        [DataRow(2, 2, true, true, true, DisplayName = "An integer is greater or equal to another integer because both are equal")]
        // <=int
        [DataRow(1, 2, false, true, true, DisplayName = "An integer is less or equal to another integer")]
        [DataRow(2, 1, false, true, false, DisplayName = "An integer is not less or equal to another integer")]
        [DataRow(1, 1, false, true, true, DisplayName = "An integer is less or equal to another integer because both are equal")]
        // >float
        [DataRow(2.8, 1.3, true, false, true, DisplayName = "A float is greater than another float")]
        [DataRow(1.3, 2.8, true, false, false, DisplayName = "A float is not greater than another float")]
        [DataRow(1.3, 1.3, true, false, false, DisplayName = "A float is not greater than another float because both are equal")]
        // <float
        [DataRow(1.3, 2.8, false, false, true, DisplayName = "A float is less than another float")]
        [DataRow(2.8, 1.3, false, false, false, DisplayName = "A float is not less than another float")]
        [DataRow(2.8, 2.8, false, false, false, DisplayName = "A float is not less than another float because both are equal")]
        // >=float
        [DataRow(2.8, 1.3, true, true, true, DisplayName = "A float is greater or equal to another float")]
        [DataRow(1.3, 2.8, true, true, false, DisplayName = "A float is not greater or equal to another float")]
        [DataRow(2.8, 2.8, true, true, true, DisplayName = "A float is greater or equal to another float because both are equal")]
        // <=float
        [DataRow(1.3, 2.8, false, true, true, DisplayName = "A float is less or equal to another float")]
        [DataRow(2.8, 1.3, false, true, false, DisplayName = "A float is not less or equal to another float")]
        [DataRow(1.3, 1.3, false, true, true, DisplayName = "A float is less or equal to another float because both are equal")]
        // >int, float
        [DataRow(2, 1.3, true, false, true, DisplayName = "An integer is greater than a float")]
        [DataRow(1, 2.1, true, false, false, DisplayName = "An integer is not greater than a float")]
        [DataRow(1, 1.0, true, false, false, DisplayName = "An integer is not greater than a float because both are equal")]
        // <int, float
        [DataRow(1, 2.1, false, false, true, DisplayName = "An integer is less than a float")]
        [DataRow(2, 1.3, false, false, false, DisplayName = "An integer is not less than a float")]
        [DataRow(1, 1.0, false, false, false, DisplayName = "An integer is not less than a float because both are equal")]
        // >=int, float
        [DataRow(2, 1.3, true, true, true, DisplayName = "An integer is greater or equal to a float")]
        [DataRow(1, 2.1, true, true, false, DisplayName = "An integer is not greater or equal to a float")]
        [DataRow(2, 2.0, true, true, true, DisplayName = "An integer is greater or equal to a float because both are equal")]
        // <=int, float
        [DataRow(1, 2.1, false, true, true, DisplayName = "An integer is less or equal to a float")]
        [DataRow(2, 1.3, false, true, false, DisplayName = "An integer is not less or equal to a float")]
        [DataRow(1, 1.0, false, true, true, DisplayName = "An integer is less or equal to a float because both are equal")]
        // >float, int
        [DataRow(2.1, 1, true, false, true, DisplayName = "A float is greater than an integer")]
        [DataRow(1.3, 2, true, false, false, DisplayName = "A float is not greater than an integer")]
        [DataRow(1.0, 1, true, false, false, DisplayName = "A float is not greater than an integer because both are equal")]
        // <float, int
        [DataRow(1.3, 2, false, false, true, DisplayName = "A float is less than an integer")]
        [DataRow(2.1, 1, false, false, false, DisplayName = "A float is not less than an integer")]
        [DataRow(1.0, 1, false, false, false, DisplayName = "A float is not less than an integer because both are equal")]
        // >=float, int
        [DataRow(2.1, 1, true, true, true, DisplayName = "A float is greater or equal to an integer")]
        [DataRow(1.3, 2, true, true, false, DisplayName = "A float is not greater or equal to an integer")]
        [DataRow(2.0, 2, true, true, true, DisplayName = "A float is greater or equal to an integer because both are equal")]
        // <=float, int
        [DataRow(1.3, 2, false, true, true, DisplayName = "A float is less or equal to an integer")]
        [DataRow(2.1, 1, false, true, false, DisplayName = "A float is not less or equal to an integer")]
        [DataRow(1.0, 1, false, true, true, DisplayName = "A float is less or equal to an integer because both are equal")]
        public void EvaluateExpression_ValidNumericType_ReturnsExpectedEvaluationResult(object leftValue, object rightValue, bool greater, bool orEquals, bool evaluationResult)
        {
            CompareObjects(leftValue, rightValue, greater, orEquals, evaluationResult);
        }

        [DataTestMethod]
        // >date
        [DataRow(637676928000000000, 637500672000000000, true, false, true, DisplayName = "A date is greater than another date")]
        [DataRow(637500672000000000, 637676928000000000, true, false, false, DisplayName = "A date is not greater than another date")]
        [DataRow(637500672000000000, 637500672000000000, true, false, false, DisplayName = "A date is not greater than another date because both are equal")]
        // <date
        [DataRow(637500672000000000, 637676928000000000, false, false, true, DisplayName = "A date is less than another date")]
        [DataRow(637676928000000000, 637500672000000000, false, false, false, DisplayName = "A date is not less than another date")]
        [DataRow(637676928000000000, 637676928000000000, false, false, false, DisplayName = "A date is not less than another date because both are equal")]
        // >=date
        [DataRow(637676928000000000, 637500672000000000, true, true, true, DisplayName = "A date is greater or equal to another date")]
        [DataRow(637500672000000000, 637676928000000000, true, true, false, DisplayName = "A date is not greater or equal to another date")]
        [DataRow(637676928000000000, 637676928000000000, true, true, true, DisplayName = "A date is greater or equal to another date because both are equal")]
        // <=date
        [DataRow(637500672000000000, 637676928000000000, false, true, true, DisplayName = "A date is less or equal to another date")]
        [DataRow(637676928000000000, 637500672000000000, false, true, false, DisplayName = "A date is not less or equal to another date")]
        [DataRow(637500672000000000, 637500672000000000, false, true, true, DisplayName = "A date is less or equal to another date because both are equal")]
        public void EvaluateExpression_ValidDateType_ReturnsExpectedEvaluationResult(long leftTicks, long rightTicks, bool greater, bool orEquals, bool evaluationResult)
        {
            var leftDate = new DateTime(leftTicks);
            var rightDate = new DateTime(rightTicks);

            CompareObjects(leftDate, rightDate, greater, orEquals, evaluationResult);
        }

        [TestMethod]
        public void EvaluateExpression_InvalidTokenToEvaluate_ReturnsFalse()
        {
            var specifiedValueToken = TestUtilities.ToJToken(100);
            var tokenToEvaluate = TestUtilities.ToJToken("aString");

            CompareObjects(specifiedValueToken, tokenToEvaluate, evaluationResult: false);
        }

        [TestMethod]
        public void EvaluateExpression_CompareDateWithNumber_ReturnsFalse()
        {
            var date = new DateTime(637500672000000000);
            var number = 100;

            CompareObjects(date, number, evaluationResult: false);
        }

        [TestMethod]
        public void EvaluateExpression_CompareNumberWithDate_ReturnsFalse()
        {
            var number = 100;
            var date = new DateTime(637500672000000000);

            CompareObjects(number, date, evaluationResult: false);
        }

        private void CompareObjects(object left, object right, bool greater = false, bool orEquals = false, bool evaluationResult = false)
        {
            var leftToken = TestUtilities.ToJToken(left);
            var rightToken = TestUtilities.ToJToken(right);

            var inequalityOperator = new InequalityOperator(leftToken, greater: greater, orEquals: orEquals);

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

            Assert.Fail("Test method should have thrown an InvalidOperationException");
        }
    }
}