// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine.UnitTests
{
    [TestClass]
    public class PowerShellRuleEngineTests
    {
        private readonly string templatesFolder = @"templates";
        private static PowerShellRuleEngine powerShellRuleEngineAllRules;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            powerShellRuleEngineAllRules = new PowerShellRuleEngine(true);
        }

        [DataTestMethod]
        [DataRow("template_and_resource_level_results.json", true, 12, new int[] { 1, 1, 1, 1, 8, 14, 17, 1, 17, 17, 1, 17 }, DisplayName = "Running all the rules against a template with errors reported in both analysis stages")]
        [DataRow("template_and_resource_level_results.json", false, 4, new int[] { 17, 17, 17, 17 }, DisplayName = "Running only the security rules against a template with errors reported in both analysis stages")]
        // TODO add test case for error, warning (rule with severity level of warning?) and informational (also rule with that severity level?)
        public void AnalyzeTemplate_ValidTemplate_ReturnsExpectedEvaluations(string templateFileName, bool runsAllRules, int expectedErrorCount, dynamic lineNumbers)
        {
            var templateFilePath = Path.Combine(templatesFolder, templateFileName);

            var template = File.ReadAllText(templateFilePath);
            var armTemplateProcessor = new ArmTemplateProcessor(template);
            var templatejObject = armTemplateProcessor.ProcessTemplate();

            var templateContext = new TemplateContext
            {
                OriginalTemplate = JObject.Parse(template),
                ExpandedTemplate = templatejObject,
                ResourceMappings = armTemplateProcessor.ResourceMappings,
                TemplateIdentifier = templateFilePath
            };

            IEnumerable<IEvaluation> evaluations;
            if (runsAllRules)
            {
                evaluations = powerShellRuleEngineAllRules.AnalyzeTemplate(templateContext);
            } else
            {
                var powerShellRuleEngineSecurityRules = new PowerShellRuleEngine(false);
                evaluations = powerShellRuleEngineSecurityRules.AnalyzeTemplate(templateContext);
            }

            var failedEvaluations = new List<PowerShellRuleEvaluation>();

            foreach (PowerShellRuleEvaluation evaluation in evaluations)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleDescription));
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.HelpUri));
                Assert.IsNotNull(evaluation.Recommendation);
                Assert.IsNotNull(evaluation.Severity);

                if (evaluation.Passed)
                {
                    Assert.IsFalse(evaluation.HasResults);
                }
                else
                {
                    Assert.IsTrue(evaluation.HasResults);
                    Assert.IsFalse(evaluation.Result.Passed);

                    failedEvaluations.Add(evaluation);
                }
            }

            Assert.AreEqual(expectedErrorCount, failedEvaluations.Count);

            Assert.AreEqual(failedEvaluations.Count, lineNumbers.Length);
            for (int errorNumber = 0; errorNumber < lineNumbers.Length; errorNumber++)
            {
                Assert.AreEqual(lineNumbers[errorNumber], failedEvaluations[errorNumber].Result.LineNumber);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AnalyzeTemplate_NullTemplateContext_ThrowsException()
        {
            powerShellRuleEngineAllRules.AnalyzeTemplate(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AnalyzeTemplate_NullTemplateIdentifier_ThrowsException()
        {
            powerShellRuleEngineAllRules.AnalyzeTemplate(new TemplateContext());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AnalyzeTemplate_NullExpandedTemplate_ThrowsException()
        {
            powerShellRuleEngineAllRules.AnalyzeTemplate(new TemplateContext());
        }
    }
}