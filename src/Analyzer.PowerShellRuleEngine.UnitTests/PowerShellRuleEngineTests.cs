// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine.UnitTests
{
    [TestClass]
    public class PowerShellRuleEngineTests
    {
        private readonly string templatesFolder = @"templates\";
        private static PowerShellRuleEngine powerShellRuleEngine;

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            powerShellRuleEngine = new PowerShellRuleEngine();
        }

        [DataTestMethod]
        [DataRow("success.json", 0, new int[] { }, DisplayName = "Base template")]
        [DataRow("error_without_line_number.json", 1, new int[] { 0 }, DisplayName = "Template with an error reported without a line number")]
        [DataRow("error_with_line_number.json", 1, new int[] { 9 }, DisplayName = "Template with an error reported with a line number")]
        [DataRow("warning.json", 1, new int[] { 0 }, DisplayName = "Template with a warning")]
        [DataRow("repeated_error_same_message_same_lines.json", 1, new int[] { 0 }, DisplayName = "Template with an error found in multiple places, reported with the same message and line number for each")]
        public void AnalyzeTemplate_ValidTemplate_ReturnsExpectedEvaluations(string templateFileName, int expectedErrorCount, dynamic lineNumbers)
        {
            var templateContext = new TemplateContext { TemplateIdentifier = templatesFolder + templateFileName };

            var evaluations = powerShellRuleEngine.AnalyzeTemplate(templateContext);

            var failedEvaluations = new List<PowerShellRuleEvaluation>();

            foreach (PowerShellRuleEvaluation evaluation in evaluations)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleDescription));
                Assert.IsNotNull(evaluation.Recommendation);

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
                Assert.IsFalse(failedEvaluations[errorNumber].RuleDescription.Contains(" on line: "));
            }
        }

        [TestMethod]
        public void AnalyzeTemplate_RepeatedErrorSameMessage_ReturnsExpectedEvaluations()
        {
            var templateContext = new TemplateContext { TemplateIdentifier = templatesFolder + "repeated_error_same_message_different_lines.json" };

            var evaluations = powerShellRuleEngine.AnalyzeTemplate(templateContext);

            var evaluationsList = evaluations.ToList();
            Assert.AreEqual(2, evaluationsList.Count);

            foreach (var evaluation in evaluationsList)
            {
                Assert.IsFalse(evaluation.Passed);
                Assert.IsFalse(evaluation.Result.Passed);
            }

            Assert.AreEqual(9, evaluationsList[0].Result.LineNumber);
            Assert.AreEqual(13, evaluationsList[1].Result.LineNumber);
        }

        [TestMethod]
        public void AnalyzeTemplate_RepeatedErrorDifferentMessage_ReturnsExpectedEvaluations()
        {
            var templateContext = new TemplateContext { TemplateIdentifier = templatesFolder + "repeated_error_different_message.json" };

            var evaluations = powerShellRuleEngine.AnalyzeTemplate(templateContext);

            var evaluationsList = evaluations.ToList();

            Assert.AreEqual(2, evaluationsList.Count);

            Assert.IsFalse(evaluationsList[0].Passed);
            Assert.IsFalse(evaluationsList[1].Passed);

            Assert.IsFalse(evaluationsList[0].Result.Passed);
            Assert.IsFalse(evaluationsList[1].Result.Passed);

            Assert.AreEqual(evaluationsList[0].RuleId, evaluationsList[1].RuleId);
            Assert.AreNotEqual(evaluationsList[0].RuleDescription, evaluationsList[1].RuleDescription);
        }

        [TestMethod]
        public void AnalyzeTemplate_MissingTTKRepository_DoesNotThrowAnException()
        {
            var TTKFolderName = "TTK";
            var wrongTTKFolderName = TTKFolderName + "2";
            var templateContext = new TemplateContext { TemplateIdentifier = templatesFolder + "error_without_line_number.json" };

            System.IO.Directory.Move(TTKFolderName, wrongTTKFolderName);

            try
            {
                var powerShellRuleEngineWrongPath = new PowerShellRuleEngine();
                var evaluations = powerShellRuleEngineWrongPath.AnalyzeTemplate(templateContext);

                Assert.AreEqual(0, evaluations.Count());
            }
            finally
            {
                // Ensure directory is moved back in case of test failure
                System.IO.Directory.Move(wrongTTKFolderName, TTKFolderName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AnalyzeTemplate_NullTemplateContext_ThrowsException()
        {
            powerShellRuleEngine.AnalyzeTemplate(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AnalyzeTemplate_NullTemplateIdentifier_ThrowsException()
        {
            powerShellRuleEngine.AnalyzeTemplate(new TemplateContext());
        }
    }
}