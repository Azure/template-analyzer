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
        private const string EmptyBaseline = @"
        [
          {
            ""kind"": ""Baseline"",
            ""metadata"": {
              ""name"": ""RepeatedRulesBaseline""
            },
            ""apiVersion"": ""github.com/microsoft/PSRule/v1"",
            ""spec"": {
              ""rule"": {
                ""exclude"": [
                ]
              }
            }
          }
        ]";

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
        [DataRow("template_and_resource_level_results.json", true, 14, new int[] { 1, 1, 1, 1, 8, 11, 14, 17, 1, 17, 17, 1, 17, 1 }, DisplayName = "Running all the rules against a template with errors reported in both analysis stages")]
        [DataRow("template_and_resource_level_results.json", false, 5, new int[] { 11, 17, 17, 17, 17 }, DisplayName = "Running only the security rules against a template with errors reported in both analysis stages")]
        // TODO add test case for error, warning (rule with severity level of warning?) and informational (also rule with that severity level?)
        public void AnalyzeTemplate_ValidTemplate_ReturnsExpectedEvaluations(string templateFileName, bool runsAllRules, int expectedErrorCount, int[] expectedLineNumbers)
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
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleName));
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleShortDescription));
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleFullDescription));
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
            foreach (int expectedLineNumber in expectedLineNumbers)
            {
                var matchingEvaluation = failedEvaluations.Find(evaluation => evaluation.Result.SourceLocation.LineNumber == expectedLineNumber);
                failedEvaluations.Remove(matchingEvaluation);
            }
            Assert.IsTrue(failedEvaluations.IsEmptyEnumerable());
        }

        [DataTestMethod]
        [DataRow(true, DisplayName = "Repeated rules are excluded when running all the rules")]
        [DataRow(false, DisplayName = "Repeated rules are excluded when running only the security rules")]
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
            // Next we validate that when RepeatedRulesBaseline has no exclusions then the test file does indeed trigger the excluded rule:
            if (runsAllRules)
            {
                var baselineLocation = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory), "baselines", "RepeatedRulesBaseline.Rule.json");
                var newBaselineLocation = baselineLocation + ".moved";
                try
                {
                    File.Move(baselineLocation, newBaselineLocation);
                    File.WriteAllText(baselineLocation, EmptyBaseline);

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

        // Sanity checks for using hardcoded AZURE_RESOURCE_ALLOWED_LOCATIONS to match the placeholder region in the template processor
        // locations used are different from the placeholder region westus2
        [TestMethod]
        [DataRow("templateWithDefaultLocation.json", DisplayName = "Template with default location")]
        [DataRow("templateWithHardcodedLocation.json", DisplayName = "Template with hardcoded location")]
        public void AnalyzeTemplate_ValidTemplate_SpecifiedLocations(string templateFileName)
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

            var evaluations = powerShellRuleEngineSecurityRules.AnalyzeTemplate(templateContext);

            Assert.IsTrue(evaluations.All(evaluation => evaluation.Passed));
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