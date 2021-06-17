// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                new JsonRuleEvaluation(null, true, new JsonRuleResult[0])
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
                new JsonRuleEvaluation(null, true, new JsonRuleResult[0])
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
                new JsonRuleEvaluation(null, true, new JsonRuleResult[0])
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
                new JsonRuleEvaluation(null, true, new JsonRuleResult[0])
                {
                    RuleDefinition = new RuleDefinition { HelpUri = "https://helpUri" }
                }
                .HelpUri);
        }

        [DataTestMethod]
        [DataRow(3, 3, DisplayName = "3/3 results are true")]
        [DataRow(3, 2, DisplayName = "2/3 results are true")]
        [DataRow(3, 0, DisplayName = "0/3 results are true")]
        [DataRow(0, 0, DisplayName = "No results")]
        public void GetResultsEvaluatedTrueFalse_ThreeResults_ExpectedNumberOfResultsReturned(int totalResults, int expectedTrue)
        {
            // Arrange
            int expectedFalse = totalResults - expectedTrue;

            List<JsonRuleResult> results = new List<JsonRuleResult>();

            for (int i = 0; i < expectedTrue; i++)
            {
                results.Add(new JsonRuleResult { Passed = true });
            }

            for (int i = 0; i < expectedFalse; i++)
            {
                results.Add(new JsonRuleResult { Passed = false });
            }

            JsonRuleEvaluation jsonRuleEvaluation = new JsonRuleEvaluation(null, true, results);

            // Act
            var resultsEvaluatedTrue = jsonRuleEvaluation.ResultsEvaluatedTrue;
            var resultsEvaluatedFalse = jsonRuleEvaluation.ResultsEvaluatedFalse;

            // Assert
            Assert.AreEqual(totalResults, results.Count());
            Assert.AreEqual(expectedTrue, resultsEvaluatedTrue.Count());
            Assert.AreEqual(expectedFalse, resultsEvaluatedFalse.Count());
        }

        [TestMethod]
        public void GetResultsEvaluatedTrueFalse_ResultsNotSet_ReturnZeroResults()
        {
            // Arrange
            JsonRuleEvaluation jsonRuleEvaluation = new JsonRuleEvaluation(null, true, new List<JsonRuleEvaluation>());

            // Act
            var resultsEvaluatedTrue = jsonRuleEvaluation.ResultsEvaluatedTrue;
            var resultsEvaluatedFalse = jsonRuleEvaluation.ResultsEvaluatedFalse;

            // Assert
            Assert.AreEqual(0, resultsEvaluatedTrue.Count());
            Assert.AreEqual(0, resultsEvaluatedFalse.Count());
        }

        [DataTestMethod]
        [DataRow(3, 3, DisplayName = "3/3 evaluations are true")]
        [DataRow(3, 2, DisplayName = "2/3 evaluations are true")]
        [DataRow(3, 0, DisplayName = "0/3 evaluations are true")]
        [DataRow(0, 0, DisplayName = "No evaluations")]
        public void GetEvaluationsEvaluatedTrueFalse_ThreeEvaluations_ExpectedNumberOfEvaluationsReturned(int totalEvaluations, int expectedTrue)
        {
            // Arrange
            int expectedFalse = totalEvaluations - expectedTrue;

            List<JsonRuleEvaluation> evaluations = new List<JsonRuleEvaluation>();

            for (int i = 0; i < expectedTrue; i++)
            {
                evaluations.Add(new JsonRuleEvaluation(null, true, new JsonRuleResult[0]));
            }

            for (int i = 0; i < expectedFalse; i++)
            {
                evaluations.Add(new JsonRuleEvaluation(null, false, new JsonRuleResult[0]));
            }

            JsonRuleEvaluation jsonRuleEvaluation = new JsonRuleEvaluation(null, true, evaluations);

            // Act
            var evaluationsEvaluatedTrue = jsonRuleEvaluation.EvaluationsEvaluatedTrue;
            var evaluationsEvaluatedFalse = jsonRuleEvaluation.EvaluationsEvaluatedFalse;

            // Assert
            Assert.AreEqual(totalEvaluations, evaluations.Count());
            Assert.AreEqual(expectedTrue, evaluationsEvaluatedTrue.Count());
            Assert.AreEqual(expectedFalse, evaluationsEvaluatedFalse.Count());
        }

        [TestMethod]
        public void GetEvaluationsEvaluatedTrueFalse_EvaluationsNotSet_ReturnZeroEvaluations()
        {
            // Arrange
            JsonRuleEvaluation jsonRuleEvaluation = new JsonRuleEvaluation(null, true, new List<JsonRuleResult>());

            // Act
            var evaluationsEvaluatedTrue = jsonRuleEvaluation.EvaluationsEvaluatedTrue;
            var evaluationsEvaluatedFalse = jsonRuleEvaluation.EvaluationsEvaluatedFalse;

            // Assert
            Assert.AreEqual(0, evaluationsEvaluatedTrue.Count());
            Assert.AreEqual(0, evaluationsEvaluatedFalse.Count());
        }

        [TestMethod]
        public void Evaluations_IterateEvaluationsMultipleTimes_IterationIsCached()
        {
            int iterationCounter = 0;

            IEnumerable<JsonRuleEvaluation> GenerateEvaluationEnumerable()
            {
                iterationCounter++;
                yield return new JsonRuleEvaluation(null, true, new JsonRuleEvaluation[0]);
            }

            var testEvaluation = new JsonRuleEvaluation(null, true, GenerateEvaluationEnumerable());

            // Call each method that would iterate Evaluations, making sure the original Enumerable
            // is only ever iterated a single time.
            _ = testEvaluation.Evaluations;
            _ = testEvaluation.EvaluationsEvaluatedFalse;
            _ = testEvaluation.EvaluationsEvaluatedTrue;
            _ = testEvaluation.HasResults;
            _ = testEvaluation.Evaluations;
            Assert.AreEqual(1, iterationCounter);
        }

        [TestMethod]
        public void Results_IterateResultsMultipleTimes_IterationIsCached()
        {
            int iterationCounter = 0;

            IEnumerable<JsonRuleResult> GenerateResultEnumerable()
            {
                iterationCounter++;
                yield return new JsonRuleResult();
            }

            var testEvaluation = new JsonRuleEvaluation(null, true, GenerateResultEnumerable());

            // Call each method that would iterate Evaluations, making sure the original Enumerable
            // is only ever iterated a single time.
            _ = testEvaluation.Results;
            _ = testEvaluation.ResultsEvaluatedFalse;
            _ = testEvaluation.ResultsEvaluatedTrue;
            _ = testEvaluation.HasResults;
            _ = testEvaluation.Results;
            Assert.AreEqual(1, iterationCounter);
        }

        [TestMethod]
        public void HasResults_NestedEvaluationsWithResults_ReturnsTrue()
        {
            var evaluationWithNestedResults = new JsonRuleEvaluation(
                null,
                true,
                new[]
                {
                    new JsonRuleEvaluation(
                        null,
                        true,
                        new []
                        {
                            new JsonRuleResult(),
                            new JsonRuleResult()
                        }),
                    new JsonRuleEvaluation(
                        null,
                        true,
                        new []
                        {
                            new JsonRuleResult()
                        })
                });

            Assert.IsTrue(evaluationWithNestedResults.HasResults);
        }

        [TestMethod]
        public void HasResults_NestedEvaluationsWithNoResults_ReturnsFalse()
        {
            var evaluationWithNestedResults = new JsonRuleEvaluation(
                null,
                true,
                new[]
                {
                    new JsonRuleEvaluation(
                        null,
                        true,
                        Array.Empty<JsonRuleEvaluation>()),
                    new JsonRuleEvaluation(
                        null,
                        true,
                        Array.Empty<JsonRuleEvaluation>())
                });

            Assert.IsFalse(evaluationWithNestedResults.HasResults);
        }

        [TestMethod]
        public void HasEvaluations_NestedEvaluationsWithEvaluations_ReturnsTrue()
        {
            var evaluationWithNestedEvaluations = new JsonRuleEvaluation(
                null,
                true,
                new[]
                {
                    new JsonRuleEvaluation(
                        null,
                        true,
                        Array.Empty<JsonRuleResult>())
                });

            Assert.IsTrue(evaluationWithNestedEvaluations.HasEvaluations);
        }

        [TestMethod]
        public void HasEvaluations_EvaluationsWithNoEvaluations_ReturnsFalse()
        {
            var evaluationWithNoEvaluations = new JsonRuleEvaluation(
                null,
                true,
                Array.Empty<JsonRuleEvaluation>());

            Assert.IsFalse(evaluationWithNoEvaluations.HasEvaluations);
        }

        [TestMethod]
        public void ScopesFound_EvaluationsWithNoEvaluations_ReturnsFalse()
        {
            var evaluationWithNoEvaluations = new JsonRuleEvaluation(
                null,
                true,
                Array.Empty<JsonRuleEvaluation>());

            Assert.IsFalse(evaluationWithNoEvaluations.ScopesFound);
        }

        [TestMethod]
        public void ScopesFound_NestedEvaluationsWithNoResults_ReturnsTrue()
        {
            var evaluationWithNestedResults = new JsonRuleEvaluation(
                null,
                true,
                new[]
                {
                    new JsonRuleEvaluation(
                        null,
                        true,
                        Array.Empty<JsonRuleEvaluation>()),
                    new JsonRuleEvaluation(
                        null,
                        true,
                        Array.Empty<JsonRuleEvaluation>())
                });

            Assert.IsTrue(evaluationWithNestedResults.ScopesFound);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullResults_ThrowsException()
        {
            new JsonRuleEvaluation(null, true, results: null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullEvaluations_ThrowsException()
        {
            new JsonRuleEvaluation(null, true, evaluations: null);
        }
    }
}
