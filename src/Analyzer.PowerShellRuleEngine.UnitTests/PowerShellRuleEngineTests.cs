// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine.UnitTests
{
    [TestClass]
    public class PowerShellRuleEngineTests
    {
        [DataTestMethod]
        [DataRow("aFilePath", DisplayName = "A description")]
        public void EvaluateRules_TestScenario_ResultExpected(string templateFilePath)
        {
            Assert.IsTrue(true);
        }
    }
}