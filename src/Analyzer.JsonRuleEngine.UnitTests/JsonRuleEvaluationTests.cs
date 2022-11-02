// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class JsonRuleEvaluationTests
    {

        [TestMethod]
        public void GetRuleId_ReturnsIdFromRule()
        {
            Assert.AreEqual(
                "testRule",
                new JsonRuleEvaluation(null, true, Enumerable.Empty<JsonRuleEvaluation>())
                {
                    RuleDefinition = new RuleDefinition { Id = "testRule" }
                }
                .RuleId);
        }

        [TestMethod]
        public void GetRuleName_ReturnsNameFromRule()
        {
            Assert.AreEqual(
                "TestRule",
                new JsonRuleEvaluation(null, true, Enumerable.Empty<JsonRuleEvaluation>())
                {
                    RuleDefinition = new RuleDefinition { Name = "TestRule" }
                }
                .RuleName);
        }

        [TestMethod]
        public void GetRuleDescription_ReturnsDescriptionFromRule()
        {
            Assert.AreEqual(
                "test rule",
                new JsonRuleEvaluation(null, true, Enumerable.Empty<JsonRuleEvaluation>())
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
                new JsonRuleEvaluation(null, true, Enumerable.Empty<JsonRuleEvaluation>())
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
                new JsonRuleEvaluation(null, true, Enumerable.Empty<JsonRuleEvaluation>())
                {
                    RuleDefinition = new RuleDefinition { HelpUri = "https://helpUri" }
                }
                .HelpUri);
        }

        [TestMethod]
        public void GetSeverity_ReturnsSeverityFromRule()
        {
            Assert.AreEqual(
                Severity.High,
                new JsonRuleEvaluation(null, true, Enumerable.Empty<JsonRuleEvaluation>())
                {
                    RuleDefinition = new RuleDefinition { Severity = Severity.High }
                }
                .Severity);
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
                        new JsonRuleResult()),
                    new JsonRuleEvaluation(
                        null,
                        true,
                        new JsonRuleResult())
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullResults_ThrowsException()
        {
            new JsonRuleEvaluation(null, true, result: null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullEvaluations_ThrowsException()
        {
            new JsonRuleEvaluation(null, true, evaluations: null);
        }
    }
}
