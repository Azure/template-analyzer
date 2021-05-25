// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine.UnitTests
{
    [TestClass]
    public class PowerShellRuleEngineTests
    {
        [DataTestMethod]
        [DataRow(@"success.json", 0, new int[] { }, DisplayName = "Base template")]
        [DataRow(@"error_without_line_number.json", 1, new int[] { 0 }, DisplayName = "Template with an error reported without a line number")]
        [DataRow(@"error_with_line_number.json", 1, new int[] { 9 }, DisplayName = "Template with an error reported with a line number")]
        [DataRow(@"warning.json", 1, new int[] { 0 }, DisplayName = "Template with a warning")]
        public void EvaluateRules_ValidTemplates_ReturnsExpectedEvaluations(string templateFileName, int expectedErrorCount, dynamic lineNumbers)
        {
            var templateFilePath = @"..\..\..\templates\" + templateFileName;

            var evaluations = PowerShellRuleEngine.EvaluateRules(templateFilePath);

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
                    Assert.AreEqual(1, evaluation.Results.ToList().Count);

                    failedEvaluations.Add(evaluation);
                }
            }

            Assert.AreEqual(expectedErrorCount, failedEvaluations.Count);

            Assert.AreEqual(failedEvaluations.Count, lineNumbers.Length);
            for (int errorNumber = 0; errorNumber < lineNumbers.Length; errorNumber++)
            {
                Assert.AreEqual(lineNumbers[errorNumber], failedEvaluations[errorNumber].Results.First().LineNumber);
            }
        }
    }
}