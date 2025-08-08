// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Utilities.UnitTests
{
    [TestClass]
    public class JsonPathResolverTests
    {
        /// <summary>
        /// Test data for tests that verify the matching of resource types
        /// Index 1: Template
        /// Index 2: Resource type to match
        /// Index 3: Indexes of the resources matched
        /// </summary>
        public static IReadOnlyList<object[]> ScenariosOfResolveResouceTypes { get; } = new List<object[]>
        {
            { new object[] { @"{ ""resources"": [
                                    { ""type"": ""Microsoft.ResourceProvider/resource1"" }
                                ] }", "Microsoft.ResourceProvider/resource1", new int[][] { new int[] { 0 } }, "1 (of 1) Matching Resource" } },
            { new object[] { @"{ ""resources"": [
                                    { ""type"": ""Microsoft.ResourceProvider/resource1"" },
                                    { ""type"": ""Microsoft.ResourceProvider/resource1"" }
                                ] }", "Microsoft.ResourceProvider/resource1", new int[][] { new int[] { 0 }, new int[] { 1 } }, "2 (of 2) Matching Resources" } },
            { new object[] { @"{ ""resources"": [
                                    { ""type"": ""Microsoft.ResourceProvider/resource1"" },
                                    { ""type"": ""Microsoft.ResourceProvider/resource2"" }
                                ] }", "Microsoft.ResourceProvider/resource2", new int[][] { new int[] { 1 } }, "1 (of 2) Matching Resources" } },
            { new object[] { @"{ ""resources"": [
                                    { ""type"": ""Microsoft.ResourceProvider/resource1"" },
                                    { ""type"": ""Microsoft.ResourceProvider/resource2"" }
                                ] }", "Microsoft.ResourceProvider/resource3", Array.Empty<int[]>(), "0 (of 2) Matching Resources" } },
            { new object[] { @"{
                ""resources"": [
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource1""
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource2"",
                        ""resources"": [
                            {
                                ""type"": ""Microsoft.ResourceProvider/resource2/resource3"",
                                ""resources"": [
                                    {
                                        ""type"": ""Microsoft.ResourceProvider/resource2/resource3/resource4""
                                    }    
                                ]
                            }
                        ]
                    }
                ]
            }", "Microsoft.ResourceProvider/resource2/resource3/resource4", new int[][] { new int[] { 1, 0, 0 } }, "1 Matching Child Resource" } }
        }.AsReadOnly();

        // Just returns the element in the last index of the array from ScenariosOfResolveResouceTypes
        public static string GetDisplayName(MethodInfo _, object[] data) => (string)data[^1];

        [DataTestMethod]
        [DataRow(null, DisplayName = "Null path")]
        [DataRow("", DisplayName = "Empty path")]
        public void Resolve_NullOrEmptyPath_ReturnsResolverWithOriginalJtoken(string path)
        {
            var jobject = JObject.Parse("{ \"Property\": \"Value\" }");

            var resolver = new JsonPathResolver(jobject, jobject.Path);

            var results = resolver.Resolve(path).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(jobject, results[0].JToken);
        }

        [DataTestMethod]
        [DataRow("nochildren", DisplayName = "Resolve one property")]
        [DataRow("onechildlevel.child2", DisplayName = "Resolve two properties deep, end of tree")]
        [DataRow("twochildlevels.child", DisplayName = "Resolve two properties deep, array returned")]
        [DataRow("twochildlevels.child2.lastprop", DisplayName = "Resolve three properties deep")]
        public void Resolve_JsonContainsPath_ReturnsResolverWithCorrectJtokenAndPath(string path)
        {
            JObject jobject = JObject.Parse(
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

            var resolver = new JsonPathResolver(jobject, jobject.Path);

            var results = resolver.Resolve(path).ToList();

            Assert.AreEqual(1, results.Count);

            // Verify correct property was resolved and resolver returns correct path
            Assert.AreEqual(path, results[0].JToken.Path, ignoreCase: true);
            Assert.AreEqual(path, results[0].Path, ignoreCase: true);
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
            JObject jobject = JObject.Parse(
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

            var resolver = new JsonPathResolver(jobject, jobject.Path);

            var arrayRegex = new Regex(@"(?<property>\w+)\[(\d|\*)\]");

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

                // Verify returned path matches JObject path
                Assert.AreEqual(resolved.JToken.Path, resolved.Path, ignoreCase: true);
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
            var jobject = JObject.Parse("{ \"Property\": \"Value\" }");

            var resolver = new JsonPathResolver(jobject, jobject.Path);

            var results = resolver.Resolve(path).ToList();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(null, results[0].JToken);
            Assert.AreEqual(path, results[0].Path);
        }

        [DataTestMethod]
        [DynamicData(nameof(ScenariosOfResolveResouceTypes), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void ResolveResourceType_JObjectWithExpectedResourcesArray_ReturnsResourcesOfCorrectType(string template, string resourceType, int[][] matchingResourceIndexes, string _)
        {
            var jToken = JObject.Parse(template);
            var resolver = new JsonPathResolver(jToken, jToken.Path);

            var resources = resolver.ResolveResourceType(resourceType).ToList();
            Assert.AreEqual(matchingResourceIndexes.Length, resources.Count);

            // Verify resources of correct type were returned
            for (int numOfResourceMatched = 0; numOfResourceMatched < matchingResourceIndexes.Length; numOfResourceMatched++)
            {
                var resource = resources[numOfResourceMatched];
                var resourceIndexes = matchingResourceIndexes[numOfResourceMatched];
                var expectedPath = "";

                for (int numOfResourceIndex = 0; numOfResourceIndex < resourceIndexes.Length; numOfResourceIndex++)
                {
                    if (numOfResourceIndex != 0)
                    {
                        expectedPath += ".";
                    }
                    expectedPath += $"resources[{resourceIndexes[numOfResourceIndex]}]";
                }

                Assert.AreEqual(expectedPath, resource.JToken.Path);
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
        public void ResolveResourceType_LanguageVersion2SymbolicNaming_ReturnsResourcesOfCorrectType()
        {
            // Test template with language version 2.0 symbolic naming (resources as object)
            var template = @"{
                ""languageVersion"": ""2.0"",
                ""resources"": {
                    ""storageAccount"": {
                        ""type"": ""Microsoft.Storage/storageAccounts"",
                        ""apiVersion"": ""2021-04-01"",
                        ""name"": ""mystorageaccount""
                    },
                    ""virtualNetwork"": {
                        ""type"": ""Microsoft.Network/virtualNetworks"",
                        ""apiVersion"": ""2021-02-01"",
                        ""name"": ""myvnet""
                    },
                    ""anotherStorage"": {
                        ""type"": ""Microsoft.Storage/storageAccounts"",
                        ""apiVersion"": ""2021-04-01"",
                        ""name"": ""anotherstorageaccount""
                    }
                }
            }";

            var jToken = JObject.Parse(template);
            var resolver = new JsonPathResolver(jToken, jToken.Path);

            // Test 1: Find all storage accounts (should return 2)
            var storageAccounts = resolver.ResolveResourceType("Microsoft.Storage/storageAccounts").ToList();
            Assert.AreEqual(2, storageAccounts.Count);
            
            // Verify the correct resources were found
            foreach (var account in storageAccounts)
            {
                Assert.AreEqual("Microsoft.Storage/storageAccounts", 
                    account.JToken.InsensitiveToken("type")?.Value<string>());
            }

            // Test 2: Find virtual networks (should return 1)
            var virtualNetworks = resolver.ResolveResourceType("Microsoft.Network/virtualNetworks").ToList();
            Assert.AreEqual(1, virtualNetworks.Count);
            Assert.AreEqual("Microsoft.Network/virtualNetworks", 
                virtualNetworks[0].JToken.InsensitiveToken("type")?.Value<string>());

            // Test 3: Find non-existent resource type (should return 0)
            var nonExistent = resolver.ResolveResourceType("Microsoft.Compute/virtualMachines").ToList();
            Assert.AreEqual(0, nonExistent.Count);

            // Test 4: Verify caching works correctly (run twice)
            var storageAccountsAgain = resolver.ResolveResourceType("Microsoft.Storage/storageAccounts").ToList();
            Assert.AreEqual(2, storageAccountsAgain.Count);
        }

        [TestMethod]
        public void ResolveResourceType_SymbolicNamingWithNestedResources_ReturnsCorrectResources()
        {
            // Test template with nested resources in language version 2.0 format.
            // Child resources are specified using both object and array formats.
            var template = @"{
                ""languageVersion"": ""2.0"",
                ""resources"": {
                    ""parentSite"": {
                        ""type"": ""Microsoft.Web/sites"",
                        ""apiVersion"": ""2021-02-01"",
                        ""name"": ""mywebsite"",
                        ""resources"": {
                            ""siteExtension"": {
                                ""type"": ""Microsoft.Web/sites/siteextensions"",
                                ""apiVersion"": ""2021-02-01"",
                                ""name"": ""myextension""
                            }
                        }
                    },
                    ""parentSite2"": {
                        ""type"": ""Microsoft.Web/sites"",
                        ""apiVersion"": ""2021-02-01"",
                        ""name"": ""mywebsite2"",
                        ""resources"": [
                            {
                                ""type"": ""Microsoft.Web/sites/siteextensions"",
                                ""apiVersion"": ""2021-02-01"",
                                ""name"": ""myextension2""
                            }
                        ]
                    }
                }
            }";

            var jToken = JObject.Parse(template);
            var resolver = new JsonPathResolver(jToken, jToken.Path);

            // Test finding parent resource
            var sites = resolver.ResolveResourceType("Microsoft.Web/sites").ToList();
            Assert.AreEqual(2, sites.Count);

            // Test finding child resource
            var extensions = resolver.ResolveResourceType("Microsoft.Web/sites/siteextensions").ToList();
            Assert.AreEqual(2, extensions.Count);
        }

        [TestMethod]
        public void Resolve_RepeatLookupForPath_UsesInternalCache()
        {
            JObject jobject = JObject.Parse(
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

            var resolver = new JsonPathResolver(jobject, jobject.Path);

            // Resolve root and compare to confirm that resolver is using the passed JObject directly (not copied).
            var rootResult = resolver.Resolve("").ToList();
            Assert.AreEqual(1, rootResult.Count);
            Assert.AreEqual<object>(jobject, rootResult[0].JToken, "Root token does not match original JObject - a copy may have been returned.");

            // Setup multiple paths to resolve to test cache retrieval
            string[] pathsToResolve = ["NoChildren", "OneChildLevel.Child", "twochildlevels.child2.lastprop"];

            // Create a 2D array to hold resolved results for each path and each resolution attempt
            List<IJsonPathResolver>[,] resolvedResults = new List<IJsonPathResolver>[2, pathsToResolve.Length];

            // Resolve each path twice. Track each result in a 2D array and compare later to confirm the resolver is using the internal cache correctly.
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < pathsToResolve.Length; j++)
                {
                    // Resolve the specific path and confirm the correct property was resolved and resolver returns correct path
                    resolvedResults[i, j] = resolver.Resolve(pathsToResolve[j]).ToList();

                    Assert.AreEqual(1, resolvedResults[i, j].Count);
                    Assert.AreEqual(pathsToResolve[j], resolvedResults[i, j][0].JToken.Path, ignoreCase: true);
                    Assert.AreEqual(pathsToResolve[j], resolvedResults[i, j][0].Path, ignoreCase: true);
                }

                // Remove all properties to confirm cached paths are used for repeat lookup.
                jobject.RemoveAll();

                // Create a new resolver to confirm paths are now unavailable
                var resolverWithEmptyJObject = new JsonPathResolver(jobject, jobject.Path);

                for (int j = 0; j < pathsToResolve.Length; j++)
                {
                    var emptyResults = resolverWithEmptyJObject.Resolve(pathsToResolve[j]).ToList();
                    Assert.AreEqual(1, emptyResults.Count);
                    Assert.IsNull(emptyResults[0].JToken);
                }
            }

            // Validate that the results from the first and second resolution of each path are the same.
            for (int i = 0; i < pathsToResolve.Length; i++)
            {
                Assert.AreEqual(1, resolvedResults[0, i].Count);
                Assert.AreEqual(1, resolvedResults[1, i].Count);
                Assert.AreEqual<object>(resolvedResults[0, i][0].JToken, resolvedResults[1, i][0].JToken);
            }
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
