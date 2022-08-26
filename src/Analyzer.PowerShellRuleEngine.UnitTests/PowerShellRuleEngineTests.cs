// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine.UnitTests
{
    [TestClass]
    public class PowerShellRuleEngineTests
    {
        private readonly string templatesFolder = @"templates";
        private static PowerShellRuleEngine powerShellRuleEngineAllRules;
        private static PowerShellRuleEngine powerShellRuleEngineSecurityRules;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            powerShellRuleEngineAllRules = new PowerShellRuleEngine(true);
            powerShellRuleEngineSecurityRules = new PowerShellRuleEngine(false);
        }

        [DataTestMethod]
        // PSRule detects errors in two analysis stages: when looking at the whole file (through the file path), and when looking at each resource (pipeline.Process(resource)):
        [DataRow("template_and_resource_level_results.json", true, 12, new int[] { 1, 1, 1, 1, 8, 14, 17, 1, 17, 17, 1, 17 }, DisplayName = "Running all the rules against a template with errors reported in both analysis stages")]
        [DataRow("template_and_resource_level_results.json", false, 4, new int[] { 17, 17, 17, 17 }, DisplayName = "Running only the security rules against a template with errors reported in both analysis stages")]
        // TODO add test case for error, warning (rule with severity level of warning?) and informational (also rule with that severity level?)
        public void AnalyzeTemplate_ValidTemplate_ReturnsExpectedEvaluations(string templateFileName, bool runsAllRules, int expectedErrorCount, dynamic expectedLineNumbers)
        {
            Assert.AreEqual(expectedErrorCount, expectedLineNumbers.Length);

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

            var evaluations = runsAllRules ? powerShellRuleEngineAllRules.AnalyzeTemplate(templateContext) : powerShellRuleEngineSecurityRules.AnalyzeTemplate(templateContext);

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

            // PSRule evaluations can change order depending on the OS:
            foreach(var expectedLineNumber in expectedLineNumbers)
            {
                var matchingEvaluation = failedEvaluations.Find(evaluation => evaluation.Result.LineNumber == expectedLineNumber);
                failedEvaluations.Remove(matchingEvaluation);
            }
            Assert.IsTrue(failedEvaluations.IsEmptyEnumerable());
        }

        [DataTestMethod]
        [DataRow(true, DisplayName = "Repeated rules are excluded when running all the rules")]
        [DataRow(true, DisplayName = "Repeated rules are excluded when running only the security rules")]
        public void AnalyzeTemplate_ValidTemplate_ExcludesRepeatedRules(bool runsAllRules)
        {
            var templateFilePath = Path.Combine(templatesFolder, "triggers_excluded_rules.json");

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

            var evaluations = runsAllRules ? powerShellRuleEngineAllRules.AnalyzeTemplate(templateContext) : powerShellRuleEngineSecurityRules.AnalyzeTemplate(templateContext);

            // AZR-000081 is the id of Azure.AppService.RemoteDebug, one of the rules excluded by RepeatedRuleBaseline,
            // because there's a JSON rule in TemplateAnalyzer that checks for the same case, TA-000002
            Assert.IsTrue(!evaluations.Any(evaluation => evaluation.RuleId == "AZR-000081"));

            // The RepeatedRulesBaseline will only be used when all rules are run
            // Otherwise SecurityBaseline is used, those rules are not in the "include" array of the baseline so they won't be executed either
            // Next we validate that when RepeatedRulesBaseline is an empty file then the test file does indeed trigger the excluded rule:
            if (runsAllRules)
            {
                var baselineLocation = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "baselines", "RepeatedRulesBaseline.Rule.json");
                var newBaselineLocation = baselineLocation + ".moved";
                try
                {
                    File.Move(baselineLocation, newBaselineLocation);
                    var emptyBaseline = File.Create(baselineLocation);
                    emptyBaseline.Close();
                    
                    evaluations = powerShellRuleEngineAllRules.AnalyzeTemplate(templateContext);
                    
                    Assert.IsTrue(evaluations.Any(evaluation => evaluation.RuleId == "AZR-000081"));
                }
                finally
                {
                    File.Delete(baselineLocation);
                    File.Move(newBaselineLocation, baselineLocation);
                }
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