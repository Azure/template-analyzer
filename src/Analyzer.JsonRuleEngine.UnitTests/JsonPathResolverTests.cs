// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

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
            var results = resolver.Resolve(path).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(jtoken, results[0].JToken);
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
            var results = resolver.Resolve(path).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(null, results[0].JToken);
        }
    }
}
