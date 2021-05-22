// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine.UnitTests
{
    [TestClass]
    public class PowerShellRuleEngineTests
    {
        [DataTestMethod]
        [DataRow(@"success.json", 0, DisplayName = "Base template")]
        [DataRow(@"error_without_line_number.json", 1, DisplayName = "Template with an error reported without a line number")]
        [DataRow(@"error_with_line_number.json", 1, DisplayName = "Template with an error reported with a line number")]
        public void EvaluateRules_ValidTemplates_ReturnsExpectedEvaluations(string templateFileName, int expectedErrorCount)
        {
            var templateFilePath = @"..\..\..\templates\" + templateFileName;

            var evaluations = PowerShellRuleEngine.EvaluateRules(templateFilePath);

            var failedRulesCount = 0;
            
            foreach (PowerShellRuleEvaluation evaluation in evaluations)
            {
                if (evaluation.Passed)
                {
                    Assert.IsFalse(evaluation.HasResults);
                }
                else
                {
                    failedRulesCount++;
                }
            }

            Assert.AreEqual(expectedErrorCount, failedRulesCount);
        }
    }
}