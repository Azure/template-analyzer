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
            var specifiedValueToken = TestUtilities.ToJToken("aNonDateString");

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
        // >date
        [DataRow("2021-09-20", "2021-02-28", true, false, true, DisplayName = "A date is greater than another date")]
        [DataRow("2021-02-28", "2021-09-20", true, false, false, DisplayName = "A date is not greater than another date")]
        [DataRow("2021-02-28", "2021-02-28", true, false, false, DisplayName = "A date is not greater than another date because both are equal")]
        // <date
        [DataRow("2021-02-28", "2021-09-20", false, false, true, DisplayName = "A date is less than another date")]
        [DataRow("2021-09-20", "2021-02-28", false, false, false, DisplayName = "A date is not less than another date")]
        [DataRow("2021-09-20", "2021-09-20", false, false, false, DisplayName = "A date is not less than another date because both are equal")]
        // >=date
        [DataRow("2021-09-20", "2021-02-28", true, true, true, DisplayName = "A date is greater or equal to another date")]
        [DataRow("2021-02-28", "2021-09-20", true, true, false, DisplayName = "A date is not greater or equal to another date")]
        [DataRow("2021-09-20", "2021-09-20", true, true, true, DisplayName = "A date is greater or equal to another date because both are equal")]
        // <=date
        [DataRow("2021-02-28", "2021-09-20", false, true, true, DisplayName = "A date is less or equal to another date")]
        [DataRow("2021-09-20", "2021-02-28", false, true, false, DisplayName = "A date is not less or equal to another date")]
        [DataRow("2021-02-28", "2021-02-28", false, true, true, DisplayName = "A date is less or equal to another date because both are equal")]
        // Wrong types
        [DataRow("aString", 100, false, false, false, DisplayName = "Invalid token to evaluate")]
        [DataRow("2021-02-28", 100, false, false, false, DisplayName = "Comparing a date with a number")]
        [DataRow(100, "2021-02-28", false, false, false, DisplayName = "Comparing a number with a date")]
        // ! (date_format_a > date_format_b)
        [DataRow("2021-02-28", "2021-02-28T00:00:00Z", true, false, false, DisplayName = "A date written with format 1 is not greater than itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28T00:00:00+00:00", true, false, false, DisplayName = "A date written with format 2 is not greater than itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28T00:00Z", true, false, false, DisplayName = "A date written with format 3 is not greater than itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28T00:00+00:00", true, false, false, DisplayName = "A date written with format 4 is not greater than itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28 00:00:00Z", true, false, false, DisplayName = "A date written with format 5 is not greater than itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28 00:00:00+00:00", true, false, false, DisplayName = "A date written with format 6 is not greater than itself wrote with the classical format")]
        // date_format_a >= date_format_b
        [DataRow("2021-02-28", "2021-02-28T00:00:00Z", true, true, true, DisplayName = "A date written with format 1 is greater or equals to itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28T00:00:00+00:00", true, true, true, DisplayName = "A date written with format 2 is greater or equals to itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28T00:00Z", true, true, true, DisplayName = "A date written with format 3 is greater or equals to itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28T00:00+00:00", true, true, true, DisplayName = "A date written with format 4 is greater or equals to itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28 00:00:00Z", true, true, true, DisplayName = "A date written with format 5 is greater or equals to itself wrote with the classical format")]
        [DataRow("2021-02-28", "2021-02-28 00:00:00+00:00", true, true, true, DisplayName = "A date written with format 6 is greater or equals to itself wrote with the classical format")]
        public void EvaluateExpression_AnyTermType_ReturnsExpectedEvaluationResult(object leftValue, object rightValue, bool greater, bool orEquals, bool evaluationResult)
        {
            CompareObjects(leftValue, rightValue, greater, orEquals, evaluationResult);
        }

        private void CompareObjects(object left, object right, bool greater = false, bool orEquals = false, bool evaluationResult = false)
        {
            var leftToken = TestUtilities.ToJToken(left);
            var rightToken = TestUtilities.ToJToken(right);

            var inequalityOperator = new InequalityOperator(rightToken, greater: greater, orEquals: orEquals);

            Assert.AreEqual(evaluationResult, inequalityOperator.EvaluateExpression(leftToken));

            Assert.AreEqual(false, inequalityOperator.EvaluateExpression(null));
        }
        
        private void ValidateInvalidOperationException(Action funct, string exceptionMessage)
        {
            try
            {
                funct();

                Assert.Fail("Test method should have thrown an InvalidOperationException");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(exceptionMessage, ex.Message);
            }
        }
    }
}