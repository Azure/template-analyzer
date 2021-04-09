// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Templates.Analyzer.Utilities.UnitTests
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
        [DataRow("nochildren", DisplayName = "Resolve one property")]
        [DataRow("onechildlevel.child2", DisplayName = "Resolve two properties deep, end of tree")]
        [DataRow("twochildlevels.child", DisplayName = "Resolve two properties deep, array returned")]
        [DataRow("twochildlevels.child2.lastprop", DisplayName = "Resolve three properties deep")]
        public void Resolve_JsonContainsPath_ReturnsResolverWithCorrectJtokenAndPath(string path)
        {
            JToken jtoken = JObject.Parse(
                @"{
                    ""NoChildren"": true,
                    ""OneChildLevel"": {
                        ""Child"": ""aValue"",
                        ""Child2"": 2
                    },
                    ""TwoChildLevels"": {
                        ""Child"": [ 0, 1, 2 ],
                        ""Child2"": {
                            ""LastProp"": true
                        }
                    },
                }");

            var resolver = new JsonPathResolver(jtoken, jtoken.Path);

            // Do twice to verify internal cache correctness
            for (int i = 0; i < 2; i++)
            {
                var results = resolver.Resolve(path).ToList();

                Assert.AreEqual(1, results.Count);

                // Verify correct property was resolved and resolver returns correct path
                Assert.AreEqual(path, results[0].JToken.Path, ignoreCase: true);
                Assert.AreEqual(path, results[0].Path, ignoreCase: true);
            }
        }

        // Combinations of wildcards are tested more extensively in JTokenExtensionsTests.cs
        [DataTestMethod]
        [DataRow("*", 3, DisplayName = "Just a wildcard")]
        [DataRow("OneChildLevel.*", 2, DisplayName = "Wildcard child")]
        [DataRow("*.child", 2, DisplayName = "Wildcard parent")]
        [DataRow("TwoChildLevels.child[*]", 3, DisplayName = "Wildcard array index")]
        [DataRow("NoChildren.*", 0, DisplayName = "Wildcard matching nothing")]
        public void Resolve_JsonContainsWildcardPath_ReturnsResolverWithCorrectJtokensAndPath(string path, int expectedCount)
        {
            JToken jtoken = JObject.Parse(
                @"{
                    ""NoChildren"": true,
                    ""OneChildLevel"": {
                        ""Child"": ""aValue"",
                        ""Child2"": 2
                    },
                    ""TwoChildLevels"": {
                        ""Child"": [ 0, 1, 2 ],
                        ""Child2"": {
                            ""LastProp"": true
                        }
                    },
                }");

            var resolver = new JsonPathResolver(jtoken, jtoken.Path);

            var arrayRegex = new Regex(@"(?<property>\w+)\[(\d|\*)\]");

            // Do twice to verify internal cache correctness
            for (int i = 0; i < 2; i++)
            {
                var results = resolver.Resolve(path).ToList();

                Assert.AreEqual(expectedCount, results.Count);

                foreach (var resolved in results)
                {
                    // Verify path on each segment
                    var expectedPath = path.Split('.');
                    var actualPath = resolved.JToken.Path.Split('.');
                    Assert.AreEqual(expectedPath.Length, actualPath.Length);
                    for (int j = 0; j < expectedPath.Length; j++)
                    {
                        var expectedSegment = expectedPath[j];
                        var actualSegment = actualPath[j];
                        var arrayMatch = arrayRegex.Match(expectedSegment);

                        if (arrayMatch.Success)
                        {
                            Assert.AreEqual(arrayMatch.Groups["property"].Value, arrayRegex.Match(actualSegment).Groups["property"].Value, ignoreCase: true);
                        }
                        else
                        {
                            Assert.IsTrue(expectedSegment.Equals("*") || expectedSegment.Equals(actualPath[j], StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    // Verify returned path matches JToken path
                    Assert.AreEqual(resolved.JToken.Path, resolved.Path, ignoreCase: true);
                }
            }
        }

        [DataTestMethod]
        [DataRow("   ", DisplayName = "Whitespace path")]
        [DataRow(".", DisplayName = "Incomplete path")]
        [DataRow("Prop", DisplayName = "Non-existant path (single level)")]
        [DataRow("Property.Value", DisplayName = "Non-existant path (sub-level doesn't exist)")]
        [DataRow("Not.Existing.Property", DisplayName = "Non-existant path (multi-level, top level doesn't exist)")]
        public void Resolve_InvalidPath_ReturnsResolverWithNullJtokenAndCorrectResolvedPath(string path)
        {
            var jtoken = JObject.Parse("{ \"Property\": \"Value\" }");

            var resolver = new JsonPathResolver(jtoken, jtoken.Path);
            var expectedPath = $"{jtoken.Path}.{path}";

            // Do twice to verify internal cache correctness
            for (int i = 0; i < 2; i++)
            {
                var results = resolver.Resolve(path).ToList();

                Assert.AreEqual(1, results.Count);
                Assert.AreEqual(null, results[0].JToken);
                Assert.AreEqual(expectedPath, results[0].Path);
            }
        }

        [DataTestMethod]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourceProvider/resource1"" } ] }", "Microsoft.ResourceProvider/resource1", new[] { 0 }, DisplayName = "1 (of 1) Matching Resource")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourceProvider/resource1"" }, { ""type"": ""Microsoft.ResourceProvider/resource1"" } ] }", "Microsoft.ResourceProvider/resource1", new[] { 0, 1 }, DisplayName = "2 (of 2) Matching Resources")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourceProvider/resource1"" }, { ""type"": ""Microsoft.ResourceProvider/resource2"" } ] }", "Microsoft.ResourceProvider/resource2", new[] { 1 }, DisplayName = "1 (of 2) Matching Resources")]
        [DataRow(@"{ ""resources"": [ { ""type"": ""Microsoft.ResourceProvider/resource1"" }, { ""type"": ""Microsoft.ResourceProvider/resource2"" } ] }", "Microsoft.ResourceProvider/resource3", new int[] { }, DisplayName = "0 (of 2) Matching Resources")]
        public void ResolveResourceType_JObjectWithExpectedResourcesArray_ReturnsResourcesOfCorrectType(string template, string resourceType, int[] matchingResourceIndexes)
        {
            var jToken = JObject.Parse(template);
            var resolver = new JsonPathResolver(jToken, jToken.Path);

            // Do twice to verify internal cache correctness
            for (int i = 0; i < 2; i++)
            {
                var resources = resolver.ResolveResourceType(resourceType).ToList();
                Assert.AreEqual(matchingResourceIndexes.Length, resources.Count);

                // Verify resources of correct type were returned
                for (int j = 0; j < matchingResourceIndexes.Length; j++)
                {
                    var resource = resources[j];
                    int resourceIndex = matchingResourceIndexes[j];
                    var expectedPath = $"resources[{resourceIndex}]";
                    Assert.AreEqual(expectedPath, resource.JToken.Path);
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

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullJToken_ThrowsException()
        {
            new JsonPathResolver(null, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullPath_ThrowsException()
        {
            new JsonPathResolver(new JObject(), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void PrivateConstructor_NullResolvedPaths_ThrowsException()
        {
            var privateConstructor =
                typeof(JsonPathResolver)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .First();

            try
            {
                privateConstructor.Invoke(new object[] { new JObject(), "path", null });
            }
            catch (TargetInvocationException e)
            {
                // When the constructor throws the exception, a TargetInvocationException exception
                // is thrown (since invocation was via reflection) that wraps the inner exception.
                throw e.InnerException;
            }
        }
    }
}
