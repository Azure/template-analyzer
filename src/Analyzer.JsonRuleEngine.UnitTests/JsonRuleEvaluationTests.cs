// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class JsonRuleEvaluationTests
    {

        [TestMethod]
        public void GetRuleName_ReturnsNameFromRule()
        {
            Assert.AreEqual(
                "testRule",
                new JsonRuleEvaluation(true, results: null)
                {
                    RuleDefinition = new RuleDefinition { Name = "testRule" }
                }
                .RuleName);
        }

        [TestMethod]
        public void GetRuleDescription_ReturnsDescriptionFromRule()
        {
            Assert.AreEqual(
                "test rule",
                new JsonRuleEvaluation(true, results: null)
                {
                    RuleDefinition = new RuleDefinition { Description = "test rule" }
                }
                .RuleDescription);
        }

        [TestMethod]
        public void GetRecommendation_ReturnsRecommendationFromRule()
        {
            Assert.AreEqual(
                "test recommendation",
                new JsonRuleEvaluation(true, results: null)
                {
                    RuleDefinition = new RuleDefinition { Recommendation = "test recommendation" }
                }
                .Recommendation);
        }

        [TestMethod]
        public void GetHelpUri_ReturnsHelpUriFromRule()
        {
            Assert.AreEqual(
                "https://helpUri",
                new JsonRuleEvaluation(true, results: null)
                {
                    RuleDefinition = new RuleDefinition { HelpUri = "https://helpUri" }
                }
                .HelpUri);
        }

        [DataTestMethod]
        [DataRow(3, 3, 0, DisplayName = "3/3 results are true, 0/3 results are false")]
        [DataRow(3, 2, 1, DisplayName = "2/3 results are true, 1/3 results are false")]
        [DataRow(3, 0, 3, DisplayName = "0/3 results are true, 3/3 results are false")]
        [DataRow(0, 0, 0, DisplayName = "No results")]
        public void GetResultsEvaluatedTrueFalse_ThreeResults_ExpectedNumberOfResultsReturned(int totalResults, int expectedTrue, int expectedFalse)
        {
            // Arrange
            List<JsonRuleResult> results = new List<JsonRuleResult>();

            for (int i = 0; i < expectedTrue; i++)
            {
                results.Add(new JsonRuleResult { Passed = true });
            }

            for (int i = 0; i < expectedFalse; i++)
            {
                results.Add(new JsonRuleResult { Passed = false });
            }

            JsonRuleEvaluation jsonRuleEvaluation = new JsonRuleEvaluation(true, results);

            // Act
            var resultsEvaluatedTrue = jsonRuleEvaluation.ResultsEvaluatedTrue;
            var resultsEvaluatedFalse = jsonRuleEvaluation.ResultsEvaluatedFalse;

            // Assert
            Assert.AreEqual(totalResults, results.Count());
            Assert.AreEqual(expectedTrue, resultsEvaluatedTrue.Count());
            Assert.AreEqual(expectedFalse, resultsEvaluatedFalse.Count());
        }

        [TestMethod]
        public void GetResultsEvaluatedTrueFalse_ResultsIsNull_ReturnNull()
        {
            // Arrange
            JsonRuleEvaluation jsonRuleEvaluation = new JsonRuleEvaluation(true, new List<JsonRuleEvaluation>());

            // Act
            var resultsEvaluatedTrue = jsonRuleEvaluation.ResultsEvaluatedTrue;
            var resultsEvaluatedFalse = jsonRuleEvaluation.ResultsEvaluatedFalse;

            // Assert
            Assert.IsNull(resultsEvaluatedTrue);
            Assert.IsNull(resultsEvaluatedFalse);
        }

        [DataTestMethod]
        [DataRow(3, 3, 0, DisplayName = "3/3 evaluations are true, 0/3 evaluations are false")]
        [DataRow(3, 2, 1, DisplayName = "2/3 evaluations are true, 1/3 evaluations are false")]
        [DataRow(3, 0, 3, DisplayName = "0/3 evaluations are true, 3/3 evaluations are false")]
        [DataRow(0, 0, 0, DisplayName = "No evaluations")]
        public void GetEvaluationsEvaluatedTrueFalse_ThreeResults_ExpectedNumberOfEvaluationsReturned(int totalEvaluations, int expectedTrue, int expectedFalse)
        {
            // Arrange
            List<JsonRuleEvaluation> evaluations = new List<JsonRuleEvaluation>();

            for (int i = 0; i < expectedTrue; i++)
            {
                evaluations.Add(new JsonRuleEvaluation(true, results: null));
            }

            for (int i = 0; i < expectedFalse; i++)
            {
                evaluations.Add(new JsonRuleEvaluation(false, results: null));
            }

            JsonRuleEvaluation jsonRuleEvaluation = new JsonRuleEvaluation(true, evaluations);

            // Act
            var evaluationsEvaluatedTrue = jsonRuleEvaluation.EvaluationsEvaluatedTrue;
            var evaluationsEvaluatedFalse = jsonRuleEvaluation.EvaluationsEvaluatedFalse;

            // Assert
            Assert.AreEqual(totalEvaluations, evaluations.Count());
            Assert.AreEqual(expectedTrue, evaluationsEvaluatedTrue.Count());
            Assert.AreEqual(expectedFalse, evaluationsEvaluatedFalse.Count());
        }

        [TestMethod]
        public void GetEvaluationsEvaluatedTrueFalse_EvaluationsIsNull_ReturnNull()
        {
            // Arrange
            JsonRuleEvaluation jsonRuleEvaluation = new JsonRuleEvaluation(true, new List<JsonRuleResult>());

            // Act
            var evaluationsEvaluatedTrue = jsonRuleEvaluation.EvaluationsEvaluatedTrue;
            var evaluationsEvaluatedFalse = jsonRuleEvaluation.EvaluationsEvaluatedFalse;

            // Assert
            Assert.IsNull(evaluationsEvaluatedTrue);
            Assert.IsNull(evaluationsEvaluatedFalse);
        }
    }
}
