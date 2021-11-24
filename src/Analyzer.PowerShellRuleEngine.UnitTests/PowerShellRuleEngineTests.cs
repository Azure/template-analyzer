// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Powershell = System.Management.Automation.PowerShell; // There's a conflict between this class name and a namespace

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine.UnitTests
{
    [TestClass]
    public class PowerShellRuleEngineTests
    {
        private readonly string templatesFolder = @"templates\";

        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var powerShell = Powershell.Create();

                powerShell.Commands.AddCommand("Set-ExecutionPolicy")
                    .AddParameter("Scope", "Process") // Affects only the current PowerShell session
                    .AddParameter("ExecutionPolicy", "Unrestricted");

                powerShell.Invoke();
            }
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
            var powerShellRuleEngine = new PowerShellRuleEngine();

            var evaluations = powerShellRuleEngine.AnalyzeTemplate(templateContext);

            var failedEvaluations = new List<PowerShellRuleEvaluation>();

            foreach (PowerShellRuleEvaluation evaluation in evaluations)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleId));
                Assert.IsFalse(string.IsNullOrWhiteSpace(evaluation.RuleDescription));
                Assert.IsNotNull(evaluation.Recommendation);
                Assert.IsNotNull(evaluation.Severity);

                if (evaluation.Passed)
                {
                    Assert.IsFalse(evaluation.HasResults);
                }
                else
                {
                    Assert.IsTrue(evaluation.HasResults);
                    Assert.AreEqual(1, evaluation.Results.Count());
                    Assert.IsFalse(evaluation.Results.First().Passed);

                    failedEvaluations.Add(evaluation);
                }
            }

            Assert.AreEqual(expectedErrorCount, failedEvaluations.Count);

            Assert.AreEqual(failedEvaluations.Count, lineNumbers.Length);
            for (int errorNumber = 0; errorNumber < lineNumbers.Length; errorNumber++)
            {
                Assert.AreEqual(lineNumbers[errorNumber], failedEvaluations[errorNumber].Results.First().LineNumber);
                Assert.IsFalse(failedEvaluations[errorNumber].RuleDescription.Contains(" on line: "));
            }
        }

        [TestMethod]
        public void AnalyzeTemplate_RepeatedErrorSameMessage_ReturnsExpectedEvaluations()
        {
            var templateContext = new TemplateContext { TemplateIdentifier = templatesFolder + "repeated_error_same_message_different_lines.json" };
            var powerShellRuleEngine = new PowerShellRuleEngine();

            var evaluations = powerShellRuleEngine.AnalyzeTemplate(templateContext);

            Assert.AreEqual(1, evaluations.Count());
            Assert.IsFalse(evaluations.First().Passed);

            var resultsList = evaluations.First().Results.ToList();

            Assert.AreEqual(2, resultsList.Count);

            Assert.IsFalse(resultsList[0].Passed);
            Assert.IsFalse(resultsList[1].Passed);

            Assert.AreEqual(9, resultsList[0].LineNumber);
            Assert.AreEqual(13, resultsList[1].LineNumber);
        }

        [TestMethod]
        public void AnalyzeTemplate_RepeatedErrorDifferentMessage_ReturnsExpectedEvaluations()
        {
            var templateContext = new TemplateContext { TemplateIdentifier = templatesFolder + "repeated_error_different_message.json" };
            var powerShellRuleEngine = new PowerShellRuleEngine();

            var evaluations = powerShellRuleEngine.AnalyzeTemplate(templateContext);

            var evaluationsList = evaluations.ToList();

            Assert.AreEqual(2, evaluationsList.Count);

            Assert.IsFalse(evaluationsList[0].Passed);
            Assert.IsFalse(evaluationsList[1].Passed);

            Assert.AreEqual(1, evaluationsList[0].Results.Count());
            Assert.AreEqual(1, evaluationsList[1].Results.Count());

            Assert.IsFalse(evaluationsList[0].Results.First().Passed);
            Assert.IsFalse(evaluationsList[1].Results.First().Passed);

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

            var powerShellRuleEngineWrongPath = new PowerShellRuleEngine();
            var evaluations = powerShellRuleEngineWrongPath.AnalyzeTemplate(templateContext);

            try
            {
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
            new PowerShellRuleEngine().AnalyzeTemplate(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AnalyzeTemplate_NullTemplateIdentifier_ThrowsException()
        {
            new PowerShellRuleEngine().AnalyzeTemplate(new TemplateContext());
        }
    }
}