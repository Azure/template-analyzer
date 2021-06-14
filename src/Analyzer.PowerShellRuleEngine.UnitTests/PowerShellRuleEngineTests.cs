// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine.UnitTests
{
    [TestClass]
    public class PowerShellRuleEngineTests
    {
        private readonly string TemplatesFolder = @"..\..\..\templates\";

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            PowerShellRuleEngine.SetExecutionPolicy();
        }

        [DataTestMethod]
        [DataRow("success.json", 0, new int[] { }, DisplayName = "Base template")]
        [DataRow("error_without_line_number.json", 1, new int[] { 0 }, DisplayName = "Template with an error reported without a line number")]
        [DataRow("error_with_line_number.json", 1, new int[] { 9 }, DisplayName = "Template with an error reported with a line number")]
        [DataRow("warning.json", 1, new int[] { 0 }, DisplayName = "Template with a warning")]
        [DataRow("repeated_error_same_message_same_lines.json", 1, new int[] { 0 }, DisplayName = "Template with an error found in multiple places, reported with the same message and line number for each")]
        public void EvaluateRules_ValidTemplate_ReturnsExpectedEvaluations(string templateFileName, int expectedErrorCount, dynamic lineNumbers)
        {
            var templateFilePath = TemplatesFolder + templateFileName;
            var powerShellRuleEngine = new PowerShellRuleEngine();

            var evaluations = powerShellRuleEngine.EvaluateRules(templateFilePath);

            var failedEvaluations = new List<PowerShellRuleEvaluation>();

            foreach (PowerShellRuleEvaluation evaluation in evaluations)
            {
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
        public void EvaluateRules_RepeatedErrorSameMessage_ReturnsExpectedEvaluations()
        {
            var templateFilePath = TemplatesFolder + "repeated_error_same_message_different_lines.json";
            var powerShellRuleEngine = new PowerShellRuleEngine();

            var evaluations = powerShellRuleEngine.EvaluateRules(templateFilePath);

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
        public void EvaluateRules_RepeatedErrorDifferentMessage_ReturnsExpectedEvaluations()
        {
            var templateFilePath = TemplatesFolder + "repeated_error_different_message.json";
            var powerShellRuleEngine = new PowerShellRuleEngine();

            var evaluations = powerShellRuleEngine.EvaluateRules(templateFilePath);

            var evaluationsList = evaluations.ToList();

            Assert.AreEqual(2, evaluationsList.Count);

            Assert.IsFalse(evaluationsList[0].Passed);
            Assert.IsFalse(evaluationsList[1].Passed);

            Assert.AreEqual(1, evaluationsList[0].Results.Count());
            Assert.AreEqual(1, evaluationsList[1].Results.Count());

            Assert.IsFalse(evaluationsList[0].Results.First().Passed);
            Assert.IsFalse(evaluationsList[1].Results.First().Passed);

            Assert.AreEqual(evaluationsList[0].RuleName, evaluationsList[1].RuleName);
            Assert.AreNotEqual(evaluationsList[0].RuleDescription, evaluationsList[1].RuleDescription);
        }

        [TestMethod]
        public void EvaluateRules_MissingTTKRepository_DoesNotThrowAnException()
        {
            var TTKFolderName = "TTK";
            var wrongTTKFolderName = TTKFolderName + "2";
            var templateFilePath = TemplatesFolder + "error_without_line_number.json";

            System.IO.Directory.Move(TTKFolderName, wrongTTKFolderName);

            var powerShellRuleEngineWrongPath = new PowerShellRuleEngine();
            var evaluations = powerShellRuleEngineWrongPath.EvaluateRules(templateFilePath);
            Assert.AreEqual(0, evaluations.Count());

            System.IO.Directory.Move(wrongTTKFolderName, TTKFolderName);

            var powerShellRuleEngine = new PowerShellRuleEngine();
            evaluations = powerShellRuleEngine.EvaluateRules(templateFilePath);
            Assert.AreEqual(1, evaluations.Count());
        }
    }
}