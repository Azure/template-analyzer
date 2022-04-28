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
        public void TestRules(TestConfiguration ruleExpectations)
        {
            // Verify test config is valid (decided in GetTests function below).
            // Reason for failure is described in RuleId.
            if (ruleExpectations.RuleId.StartsWith("Invalid test"))
                Assert.Fail(ruleExpectations.RuleId);

            // Get template to analyze
            Assert.IsTrue(File.Exists(ruleExpectations.Template), message: $"Error: {ruleExpectations.Template} does not exist");
            var testTemplate = File.ReadAllText(ruleExpectations.Template);

            // Analyze template
            var evaluations = templateAnalyzer.AnalyzeTemplate(testTemplate);

            // Find any instances of the rule being tested
            var thisRuleEvaluations = evaluations.Where(e => e.RuleId.Equals(ruleExpectations.RuleId, StringComparison.OrdinalIgnoreCase)).ToList();

            // Get all lines reported as failed
            var failingLines = thisRuleEvaluations
                .Where(e => !e.Passed)
                .SelectMany(e => GetAllFailedLines(e))
                .ToList();

            failingLines.Sort();

            // Verify all expected lines are reported
            var expectedLines = ruleExpectations.ReportedFailures.Select(failure => failure.LineNumber).ToList();
            expectedLines.Sort();
            Assert.IsTrue(failingLines.SequenceEqual(expectedLines),
                "Expected failing lines do not match actual failed lines." + Environment.NewLine +
                $"Expected: [{string.Join(",", expectedLines)}]  Actual: [{string.Join(",", failingLines)}]" +
                (failingLines.Count > 0 ? "" : Environment.NewLine + "(Do the test directory and test config have the same name as the RuleId being tested?)"));
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
            var testDirectories = Directory.GetDirectories("Tests");
            foreach (var testDirectoryName in testDirectories)
            {
                var ruleId= testDirectoryName.Split(Path.DirectorySeparatorChar)[^1];
                var testConfigFile = Path.Combine(testDirectoryName, ruleId + ".json");

                if (!File.Exists(testConfigFile))
                {
                    yield return InvalidTestConfig(testDirectoryName, "Directory and inner test configuration file must both be named the same as the RuleId being tested.");
                    continue;
                }

                TestConfiguration[] tests = null;
                string errorMessage = null;

                try
                {
                    tests = JsonConvert.DeserializeObject<TestConfiguration[]>(File.ReadAllText(testConfigFile));
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                }

                if (errorMessage != null)
                {
                    // yield return is not allowed inside a catch block.
                    yield return InvalidTestConfig(testDirectoryName, errorMessage);
                    continue;
                }
                
                foreach (var test in tests)
                {
                    if (string.IsNullOrEmpty(test.Template) || test.ReportedFailures == null)
                    {
                        yield return InvalidTestConfig(testDirectoryName,
                            test.ReportedFailures == null
                            ? "No reported failures were specified.  If no failures are expected, assign an empty array to ReportedFailures property."
                            : "No template file was specified to analyze - make sure the 'Template' property is set in the test config.");
                        continue;
                    }

                    test.DisplayName = $"{ruleId} - {test.Template}";
                    test.RuleId = ruleId;
                    test.Template = Path.Combine(testDirectoryName, test.Template);
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
        public static string GetTestDisplayName(MethodInfo _, object[] input) => input?[0] is TestConfiguration test ? test.DisplayName : "Unknown";

        /// <summary>
        /// Create a <see cref="TestConfiguration"/> that signals to the test runner that this test is invalid for some reason.
        /// This is done so that invalid tests still show up as a failed test in the results, instead of breaking the whole
        /// test run for built-in rules, requiring the developer to decipher what failed during test selection.
        /// </summary>
        /// <param name="test">The test that's invalid.</param>
        /// <param name="reason">A description of why the test is invalid.</param>
        /// <returns>The failed configuration in an object array that can be used by the test framework.</returns>
        private static object[] InvalidTestConfig(string test, string reason) =>
            new object[] {
                new TestConfiguration
                {
                    DisplayName = test,
                    RuleId = $"Invalid test: {reason}"
                }
            };
    }
}
