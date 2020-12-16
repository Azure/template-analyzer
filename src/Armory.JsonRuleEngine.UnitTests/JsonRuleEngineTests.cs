// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Armory.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class JsonRuleEngineTests
    {
        [DataTestMethod]
        [DataRow(@"[
	            {
                    ""name"": ""RuleName"",
                    ""description"": ""Rule description"",
                    ""recommendation"": ""Recommendation"",
                    ""helpUri"": ""Uri"",
                    ""evaluation"": {
                        ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                        ""path"": ""properties.somePath"",
                        ""hasValue"": true
                    }
                }
            ]",
            @"{
                ""resources"": [
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource0"",
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    }
                ]
            }", 1, 0, DisplayName = "1 Rule with 1 Expected Result and 0 Expected Failing Result")]
        [DataRow(@"[
	            {
                    ""name"": ""RuleName"",
                    ""description"": ""Rule description"",
                    ""recommendation"": ""Recommendation"",
                    ""helpUri"": ""Uri"",
                    ""evaluation"": {
                        ""resourceType"": ""Microsoft.ResourceProvider/resource0"",
                        ""path"": ""properties.someOtherPath"",
                        ""hasValue"": true
                    }
                },
                {
                    ""name"": ""RuleName"",
                    ""description"": ""Rule description"",
                    ""recommendation"": ""Recommendation"",
                    ""helpUri"": ""Uri"",
                    ""evaluation"": {
                        ""path"": ""$schema"",
                        ""hasValue"": true
                    }
                }
            ]",
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""resources"": [
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource0"",
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    }
                ]
            }", 1, 1, DisplayName = "2 Rules with 1 Expected Passing Result and 1 Expected Failing Result")]
        public void Run_ValidInputs_ReeturnsExpectedResults(string rules, string template, int numberOfExpectedPassedResults, int numberOfExpectedFailedResults)
        {
            TemplateContext templateContext = new TemplateContext { 
                OriginalTemplate = JObject.Parse(template), 
                ExpandedTemplate = JObject.Parse(template), 
                IsMainTemplate = true };

            var ruleEngine = new Armory.JsonEngine.JsonRuleEngine();
            var results = ruleEngine.EvaluateRules(templateContext, rules);

            Assert.AreEqual(numberOfExpectedFailedResults + numberOfExpectedPassedResults, results.Count());
            Assert.AreEqual(numberOfExpectedFailedResults, results.Where(result => result.Passed == false).Count());
            Assert.AreEqual(numberOfExpectedPassedResults, results.Where(result => result.Passed == true).Count());
        }
    }
}