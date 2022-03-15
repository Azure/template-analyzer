// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Core.BuiltInRuleTests
{
    [TestClass]
    public class TestRunner
    {
        Dictionary<string, IEnumerable<IEvaluation>> templateEvaluations = new();
        static TemplateAnalyzer templateAnalyzer;

        [ClassInitialize]
        public static void Initialize(TestContext testContext)
        {
            templateAnalyzer = TemplateAnalyzer.Create();
        }

        /// <summary>
        /// Runs a test defined in a test configuration file in the Tests directory.
        /// </summary>
        /// <param name="ruleExpectations">The test configuration to run.</param>
        [DataTestMethod]
        [DynamicData(nameof(GetTests), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetTestDisplayName))]
        public void TestRule(TestConfiguration ruleExpectations)
        {
            // Get template to analyze
            var testTemplatePath = Path.Combine("TestTemplates", $"{ruleExpectations.Template}.badtemplate");
            var testTemplate = File.ReadAllText(testTemplatePath);

            // If not already analyzed, analyze it and store evaluations
            if (!templateEvaluations.TryGetValue(testTemplatePath, out var evaluations))
            {
                evaluations = templateAnalyzer.AnalyzeTemplate(testTemplate);
                templateEvaluations[testTemplatePath] = evaluations;
            }

            // Find any instances of the rule being tested
            // Exception containing "Sequence contains no elements" likely means you did 
            // not name the test file the same name as the rule
            var thisRuleEvaluations = evaluations.Where(e => e.RuleId.Equals(ruleExpectations.TestName, StringComparison.OrdinalIgnoreCase)).ToList();

            // Get all lines reported as failed
            var failingLines = thisRuleEvaluations
                .Where(e => !e.Passed)
                .SelectMany(e => GetAllFailedLines(e))
                .ToHashSet();

            // Verify all expected lines are reported
            var expectedLines = ruleExpectations.ReportedFailures.Select(failure => failure.LineNumber).ToHashSet();
            Assert.IsTrue(failingLines.SetEquals(expectedLines),
                "Expected failing lines do not match actual failed lines.  " +
                $"Expected: [{string.Join(",", expectedLines)}]  Actual: [{string.Join(",", failingLines)}]");
        }

        /// <summary>
        /// Recursively get all <see cref="IResult"/> objects inside an <see cref="IEvaluation"/>.
        /// </summary>
        /// <param name="evaluation">The <see cref="IEvaluation"/> to get the <see cref="IResult"/> from.</param>
        /// <returns>All <see cref="IResult"/>s in the <see cref="IEvaluation"/>.</returns>
        private IEnumerable<int> GetAllFailedLines(IEvaluation evaluation)
        {
            if (!evaluation.Result?.Passed ?? false)
            {
                yield return evaluation.Result.LineNumber;
            }
            foreach (var subEvaluation in evaluation.Evaluations.Where(e => !e.Passed))
            {
                foreach (var line in GetAllFailedLines(subEvaluation))
                {
                    yield return line;
                }
            }
        }

        /// <summary>
        /// Reads all test configuration files from the Tests directory and provides them to <see cref="TestRule"/> to run the test.
        /// </summary>
        /// <returns>All the test configurations to run tests with.</returns>
        public static IEnumerable<object[]> GetTests()
        {
            var testConfigurationFiles = Directory.GetFiles("Tests");
            foreach (var testConfigFile in testConfigurationFiles)
            {
                var tests = JsonConvert.DeserializeObject<TestConfiguration[]>(File.ReadAllText(testConfigFile));
                foreach (var test in tests)
                {
                    test.TestName = Path.GetFileNameWithoutExtension(testConfigFile);
                    yield return new object[] { test }; 
                }
            }
        }

        /// <summary>
        /// Generates the test display name based on the test configuration used for a test.
        /// </summary>
        /// <param name="_">Not used, but input is required in order to match the function signature expected by MSTest.</param>
        /// <param name="input">The test input given to <see cref="TestRule"/>.</param>
        /// <returns>The display name of the test.</returns>
        public static string GetTestDisplayName(MethodInfo _, object[] input) => (input?[0] as TestConfiguration)?.TestName ?? "Unknown";
    }
}
