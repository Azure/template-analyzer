// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Armory.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class JsonRuleResultTests
    {
        [TestMethod]
        public void GetRuleName_ReturnsNameFromRule()
        {
            Assert.AreEqual(
                "testRule",
                new JsonRuleResult
                    {
                        RuleDefinition = new RuleDefinition { Name = "testRule" }
                    }
                .RuleName);
        }

        [TestMethod]
        public void GetRuleDescription_ReturnsDescriptionFromRule()
        {
            Assert.AreEqual(
                "test rule",
                new JsonRuleResult
                    {
                        RuleDefinition = new RuleDefinition { Description = "test rule" }
                    }
                .RuleDescription);
        }

        [TestMethod]
        public void GetRecommendation_ReturnsRecommendationFromRule()
        {
            Assert.AreEqual(
                "test recommendation",
                new JsonRuleResult
                    {
                        RuleDefinition = new RuleDefinition { Recommendation = "test recommendation" }
                    }
                .Recommendation);
        }

        [TestMethod]
        public void GetHelpUri_ReturnsHelpUriFromRule()
        {
            Assert.AreEqual(
                "https://helpUri",
                new JsonRuleResult
                    {
                        RuleDefinition = new RuleDefinition { HelpUri = "https://helpUri" }
                    }
                .HelpUri);
        }
    }
}
