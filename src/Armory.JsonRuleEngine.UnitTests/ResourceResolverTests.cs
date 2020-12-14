// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class ResourceResolverTests
    {
        [DataTestMethod]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourcProvider/resource"" } ] }", "Microsoft.ResourcProvider/resource", 1, DisplayName = "1 (of 1) Matching Resource")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourcProvider/resource"" }, { ""type"": ""Microsoft.ResourcProvider/resource"" } ] }", "Microsoft.ResourcProvider/resource", 2, DisplayName = "1 (of 2) Matching Resources")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourcProvider/resource"" }, { ""type"": ""Microsoft.ResourcProvider/resource2"" } ] }", "Microsoft.ResourcProvider/resource", 1, DisplayName = "1 (of 2) Matching Resources")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourcProvider/resource"" }, { ""type"": ""Microsoft.ResourcProvider/resource2"" } ] }", "Microsoft.ResourcProvider/resource3", 0, DisplayName = "0 (of 2) Matching Resources")]
        public void SetResources_TemplateWithResources_SetResourcesInResolver(string template, string resourceType, int expectedNumberOfMatchingResources)
        {
            var jToken = JObject.Parse(template);

            var resourceResolver = new ResourceResolver(resourceType, jToken);

            Assert.AreEqual(expectedNumberOfMatchingResources, resourceResolver.Resources.Count);
        }
    }
}