// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class JsonPathResolverTests
    {
        [DataTestMethod]
        [DataRow(null, DisplayName = "Null path")]
        [DataRow("", DisplayName = "Empty path")]
        public void Resolve_NullOrEmptyPath_ReturnsResolverWithOriginalJtoken(string path)
        {
            var jtoken = JObject.Parse("{ \"Property\": \"Value\" }");

            var resolver = new JsonPathResolver(jtoken, jtoken.Path);

            // Do twice to verify internal cache correctness
            for (int i = 0; i < 2; i++)
            {
                var results = resolver.Resolve(path).ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(jtoken, results[0].JToken); 
            }
        }

        [DataTestMethod]
        [DataRow("   ", DisplayName = "Whitespace path")]
        [DataRow(".", DisplayName = "Incomplete path")]
        [DataRow("Prop", DisplayName = "Non-existant path (single level)")]
        [DataRow("Property.Value", DisplayName = "Non-existant path (sub-level doesn't exist)")]
        [DataRow("Not.Existing.Property", DisplayName = "Non-existant path (multi-level, top level doesn't exist)")]
        public void Resolve_InvalidPath_ReturnsResolverWithNullJtoken(string path)
        {
            var jtoken = JObject.Parse("{ \"Property\": \"Value\" }");

            var resolver = new JsonPathResolver(jtoken, jtoken.Path);

            // Do twice to verify internal cache correctness
            for (int i = 0; i < 2; i++)
            {
                var results = resolver.Resolve(path).ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(null, results[0].JToken); 
            }
        }

        [DataTestMethod]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourcProvider/resource"" } ] }", "Microsoft.ResourcProvider/resource", 1, DisplayName = "1 (of 1) Matching Resource")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourcProvider/resource"" }, { ""type"": ""Microsoft.ResourcProvider/resource"" } ] }", "Microsoft.ResourcProvider/resource", 2, DisplayName = "1 (of 2) Matching Resources")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourcProvider/resource"" }, { ""type"": ""Microsoft.ResourcProvider/resource2"" } ] }", "Microsoft.ResourcProvider/resource", 1, DisplayName = "1 (of 2) Matching Resources")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourcProvider/resource"" }, { ""type"": ""Microsoft.ResourcProvider/resource2"" } ] }", "Microsoft.ResourcProvider/resource3", 0, DisplayName = "0 (of 2) Matching Resources")]
        public void ResolveResourceType_JObjectWithExpectedResourcesArray_ReturnsResourcesOfCorrectType(string template, string resourceType, int expectedNumberOfMatchingResources)
        {
            var jToken = JObject.Parse(template);
            var resolver = new JsonPathResolver(jToken, jToken.Path);

            // Do twice to verify internal cache correctness
            for (int i = 0; i < 2; i++)
            {
                var resources = resolver.ResolveResourceType(resourceType).ToList();
                Assert.AreEqual(expectedNumberOfMatchingResources, resources.Count);

                // Verify resources of correct type were returned
                foreach (var resource in resources)
                {
                    Assert.AreEqual(resourceType, resource.JToken["type"].Value<string>());
                }
            }
        }

        [DataTestMethod]
        [DataRow("string", DisplayName = "Resources is a string")]
        [DataRow(1, DisplayName = "Resources is an integer")]
        [DataRow(true, DisplayName = "Resources is a boolean")]
        [DataRow(new[] { 1, 2, 3 }, DisplayName = "Resources is an array of ints")]
        [DataRow(new[] { "1", "2", "3" }, DisplayName = "Resources is an array of ints")]
        public void ResolveResourceType_JObjectWithResourcesNotArrayOfObjects_ReturnsEmptyEnumerable(object value)
        {
            var jToken = JObject.Parse(
                string.Format("{{ \"resources\": {0} }}",
                JsonConvert.SerializeObject(value)));

            Assert.AreEqual(0, new JsonPathResolver(jToken, jToken.Path).ResolveResourceType("anything").Count());
        }
    }
}
