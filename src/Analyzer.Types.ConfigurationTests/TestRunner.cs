// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.Core;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Analyzer.Types.ConfigurationTests
{
    [TestClass]
    public class TestRunner
    {
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
            // Get configuration to analyze
            var testConfigurationPath = Path.Combine("TestConfigurations", $"{ruleExpectations.Configuration}.testconfiguration");

            // Get template to analyze
            var testTemplatePath = Path.Combine("TestTemplates", $"{ruleExpectations.Template}.badtemplate");
            var testTemplate = File.ReadAllText(testTemplatePath);

            // Run the analyzer
            templateAnalyzer.FilterRules(new FileInfo(testConfigurationPath));
            var results = templateAnalyzer.AnalyzeTemplate(testTemplate);

            // Extract Id and Severity list
            var thisRuleEvaluationIds = results.ToList().Where(e => !e.Passed).Select(e => e.RuleId).ToHashSet();
            var thisRuleEvaluationSeverities = results.ToList().Where(e => !e.Passed).Select(e => e.Severity).ToHashSet();

            // Verify all expected lines are reported
            var expectedIds = ruleExpectations.ReportedFailures.Select(failure => failure.Id).ToHashSet();
            var expectedSeverities = ruleExpectations.ReportedFailures.Select(failure => failure.Severity).ToHashSet();

            Assert.IsTrue(thisRuleEvaluationIds.SetEquals(expectedIds),
                "Expected failing rule Ids do not match actual failed lines.  " +
                $"Expected: [{string.Join(",", expectedIds)}]  Actual: [{string.Join(",", thisRuleEvaluationIds)}]");
            Assert.IsTrue(thisRuleEvaluationSeverities.SetEquals(expectedSeverities),
                "Expected failing rule Severities do not match actual failed lines.  " +
                $"Expected: [{string.Join(",", expectedSeverities)}]  Actual: [{string.Join(",", thisRuleEvaluationSeverities)}]");
        }

        /// <summary>
        /// Recursively get all <see cref="IResult"/> objects inside an <see cref="IEvaluation"/>.
        /// </summary>
        /// <param name="evaluation">The <see cref="IEvaluation"/> to get the <see cref="IResult"/> from.</param>
        /// <returns>All <see cref="IResult"/>s in the <see cref="IEvaluation"/>.</returns>
        private IEnumerable<IResult> GetAllResults(IEvaluation evaluation)
        {
            foreach (var result in evaluation.Results)
            {
                yield return result;
            }
            foreach (var subEvaluation in evaluation.Evaluations)
            {
                foreach (var subResult in GetAllResults(subEvaluation))
                {
                    yield return subResult;
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
