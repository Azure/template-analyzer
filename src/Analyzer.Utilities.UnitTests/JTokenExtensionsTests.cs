// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities.UnitTests
{
    [TestClass]
    public class JTokenExtensionsTests
    {
        private readonly JObject testJObject = JObject.Parse(@"
            {
                ""topLevel1"": {
                    ""subLevel1"": {
                        ""intArray"": [0, 1, 2]
                    },
                    ""subLevel2"": {
                        ""someNode"": false,
                        ""otherNode"": true
                    }
                },
                ""topLevel2"": {
                    ""subLevel1"": {
                        ""objArray"": [
                            {
                                ""node1"": ""valueOne""
                            },
                            {
                                ""anotherNode"": 3,
                                ""node2"": true
                            }
                        ]
                    },
                    ""subLevel2"": {
                        ""node1"": ""string"",
                        ""node2"": {
                            ""someProp"": 2
                        },
                        ""node3"": true
                    },
                    ""wildcardArray"": [
                        {
                            ""node1"": ""aValue""
                        },
                        {
                            ""nodeTwo"": ""valTwo"",
                            ""node2"": {
                                ""someProp"": ""propVal""
                            }
                        }
                    ]
                },
                ""topLevel3"": {
                    ""whitespaceNodes"": {
                        """": {
                            ""end"": 1
                        },
                        ""   "": {
                            ""end"": 2
                        },
                        "" padding "": {
                            ""end"": 3
                        }
                    }
                }
            }");

        /// <summary>
        /// Test data for tests that verify behavior for direct paths (paths without wildcards).
        /// Index 1: The path to be tested.
        /// Index 2: Whether the end token in path is expected to be found.
        ///          (This value is only used in tests where InsensitivePathNotFoundBehavior is NOT LastValid.)
        /// Index 3: An object array of the literal child elements in the test JSON corresponding to the path to be tested.
        /// Index 4: Test display name.  This is just so GetDisplayName() can do a lookup, and is not used in the test itself.
        /// </summary>
        public static IReadOnlyList<object[]> DirectPathTestScenarios { get; } = new List<object[]>
        {
            new object[] { "topLevel2", true, new object[] { "topLevel2" }, "Direct child" },
            new object[] { "topLevel1.subLevel2", true, new object[] { "topLevel1", "subLevel2" }, "Two levels deep" },
            new object[] { "topLevel2.subLevel2.node2", true, new object[] { "topLevel2", "subLevel2", "node2" }, "Three levels deep" },
            new object[] { "TOPLevel2.subLEVEL2.nODe2", true, new object[] { "topLevel2", "subLevel2", "node2" }, "Different casing in path" },
            new object[] { "topLevel3.subLevel2", false, new object[] { "topLevel3" }, "A node isn't found" },
            new object[] { "topLevel2.subLevel1.objArray[1].anotherNode", true, new object[] { "topLevel2", "subLevel1", "objArray", 1, "anotherNode" }, "Indexing into array" },
            new object[] { "topLevel1.subLevel1.intArray[3]", false, new object[] { "topLevel1", "subLevel1", "intArray" }, "Indexing out of bounds into array" },
            new object[] { "topLevel1.subLevel2[2]", false, new object[] { "topLevel1", "subLevel2" }, "Indexing node that isn't an array" },
            new object[] { "topLevel2.subLevel1.objArray", true, new object[] { "topLevel2", "subLevel1", "objArray" }, "Not specifying an index for an array" },
            new object[] { "topLevel3.whitespaceNodes..end", true, new object[] { "topLevel3", "whitespaceNodes", "", "end" }, "Selecting empty node name" },
            new object[] { "topLevel3.whitespaceNodes.   .end", true, new object[] { "topLevel3", "whitespaceNodes", "   ", "end" }, "Selecting whitespace node name" },
            new object[] { "topLevel3.whitespaceNodes. padding .end", true, new object[] { "topLevel3", "whitespaceNodes", " padding ", "end" }, "Selecting node name with whitespace padding" },
        }.AsReadOnly();

        // Just returns the element in the last index of the array from DirectPathTestScenarios
        public static string GetDisplayName(MethodInfo _, object[] data) => (string)data[^1];

        [DataTestMethod]
        [DynamicData(nameof(DirectPathTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveToken_VariousPathsWithLastValidBehavior_ReturnsCorrectToken(string path, bool _, object[] pathToExpectedToken, string __)
        {
            var actualToken = testJObject.InsensitiveToken(path, InsensitivePathNotFoundBehavior.LastValid);

            JToken expectedToken = testJObject;
            foreach (var node in pathToExpectedToken)
            {
                expectedToken = expectedToken[node];
            }

            Assert.AreEqual(expectedToken, actualToken);
        }

        [DataTestMethod]
        [DynamicData(nameof(DirectPathTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveToken_VariousPathsWithNullBehavior_ReturnsCorrectToken(string path, bool isPathFullyResolved, object[] pathToExpectedToken, string _)
        {
            // Default behavior if not specified is InsensitivePathNotFoundBehavior.Null
            var actualToken = testJObject.InsensitiveToken(path);

            JToken expectedToken = null;
            if (isPathFullyResolved)
            {
                expectedToken = testJObject;
                foreach (var node in pathToExpectedToken)
                {
                    expectedToken = expectedToken[node];
                }
            }

            Assert.AreEqual(expectedToken, actualToken);
        }

        [DataTestMethod]
        [DynamicData(nameof(DirectPathTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveToken_VariousPathsWithErrorBehavior_ReturnsCorrectTokenOrThrows(string path, bool isPathFullyResolved, object[] pathToExpectedToken, string __)
        {
            try
            {
                var actualToken = testJObject.InsensitiveToken(path, InsensitivePathNotFoundBehavior.Error);

                // If no exception is thrown, verify one wasn't expected.
                Assert.IsTrue(isPathFullyResolved);

                JToken expectedToken = testJObject;
                foreach (var node in pathToExpectedToken)
                {
                    expectedToken = expectedToken[node];
                }

                // Verify token returned.
                Assert.AreEqual(expectedToken, actualToken);
            }
            catch
            {
                // Exception thrown - verify it is expected.
                Assert.IsFalse(isPathFullyResolved);
            }
        }

        [TestMethod]
        public void InsensitiveToken_NullToken_BehavesAsSpecifiedInCall()
        {
            Assert.IsNull(((JToken)null).InsensitiveToken("path"));
            Assert.IsNull(((JToken)null).InsensitiveToken("path", InsensitivePathNotFoundBehavior.LastValid));

            try
            {
                ((JToken)null).InsensitiveToken("path", InsensitivePathNotFoundBehavior.Error);
                Assert.Fail();
            }
            catch { }
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InsensitiveToken_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsError_ArgumentNullExceptionThrown()
        {
            JTokenExtensions.InsensitiveToken(null, "someProperty", InsensitivePathNotFoundBehavior.Error);
        }

        [TestMethod]
        public void InsensitiveToken_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsLastValid_NullIsReturned()
        {
            Assert.IsNull(JTokenExtensions.InsensitiveToken(null, "path", InsensitivePathNotFoundBehavior.LastValid));
        }

        [TestMethod]
        public void InsensitiveToken_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsNull_NullIsReturned()
        {
            Assert.IsNull(JTokenExtensions.InsensitiveToken(null, "path"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InsensitiveToken_PathIsNullAndInsensitivePathNotFoundBehaviorIsError_ArgumentExceptionThrown()
        {
            testJObject.InsensitiveToken(null, InsensitivePathNotFoundBehavior.Error);
        }

        [TestMethod]
        public void InsensitiveToken_PathIsNullAndInsensitivePathNotFoundBehaviorIsLastValid_ReturnsOriginalJson()
        {
            Assert.AreEqual(testJObject, testJObject.InsensitiveToken(null, InsensitivePathNotFoundBehavior.LastValid));
        }

        [TestMethod]
        public void InsensitiveToken_PathIsNullAndInsensitivePathNotFoundBehaviorIsNull_NullIsReturned()
        {
            Assert.IsNull(testJObject.InsensitiveToken(null, InsensitivePathNotFoundBehavior.Null));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InsensitiveToken_BehaviorIsNotDefined_ThrowsException()
        {
            testJObject.InsensitiveToken(null, (InsensitivePathNotFoundBehavior)99);
        }

        [DataTestMethod]
        [DynamicData(nameof(DirectPathTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveTokens_VariousPathsWithLastValidBehavior_ReturnsCorrectToken(string path, bool _, object[] pathToExpectedToken, string __)
        {
            // This test is logically identical to InsensitiveToken_VariousPathsWithLastValidBehavior_ReturnsCorrectToken,
            // but the test calls InsensitiveTokens instead of InsensitiveToken.
            var actualToken = testJObject.InsensitiveTokens(path, InsensitivePathNotFoundBehavior.LastValid).First();

            JToken expectedToken = testJObject;
            foreach (var node in pathToExpectedToken)
            {
                expectedToken = expectedToken[node];
            }

            Assert.AreEqual(expectedToken, actualToken);
        }

        [DataTestMethod]
        [DynamicData(nameof(DirectPathTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveTokens_VariousPathsWithNullBehavior_ReturnsCorrectToken(string path, bool isPathFullyResolved, object[] pathToExpectedToken, string _)
        {
            // This test is logically identical to InsensitiveToken_VariousPathsWithNullBehavior_ReturnsCorrectToken,
            // but the test calls InsensitiveTokens instead of InsensitiveToken.
            // Default behavior if not specified is InsensitivePathNotFoundBehavior.Null
            var actualToken = testJObject.InsensitiveTokens(path).First();

            JToken expectedToken = null;
            if (isPathFullyResolved)
            {
                expectedToken = testJObject;
                foreach (var node in pathToExpectedToken)
                {
                    expectedToken = expectedToken[node];
                }
            }

            Assert.AreEqual(expectedToken, actualToken);
        }

        [DataTestMethod]
        [DynamicData(nameof(DirectPathTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveTokens_VariousPathsWithErrorBehavior_ReturnsCorrectTokenOrThrows(string path, bool isPathFullyResolved, object[] pathToExpectedToken, string __)
        {
            // This test is logically identical to InsensitiveToken_VariousPathsWithErrorBehavior_ReturnsCorrectTokenOrThrows,
            // but the test calls InsensitiveTokens instead of InsensitiveToken.
            try
            {
                var actualToken = testJObject.InsensitiveTokens(path, InsensitivePathNotFoundBehavior.Error).First();

                // If no exception is thrown, verify one wasn't expected.
                Assert.IsTrue(isPathFullyResolved);

                JToken expectedToken = testJObject;
                foreach (var node in pathToExpectedToken)
                {
                    expectedToken = expectedToken[node];
                }

                // Verify token returned.
                Assert.AreEqual(expectedToken, actualToken);
            }
            catch
            {
                // Exception thrown - verify it is expected.
                Assert.IsFalse(isPathFullyResolved);
            }
        }

        [TestMethod]
        public void InsensitiveTokens_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsNullWithNullPath_SingleNullIsReturned()
        {
            var tokens = JTokenExtensions.InsensitiveTokens(null, null).ToList();
            Assert.AreEqual(1, tokens.Count);
            Assert.IsNull(tokens[0]);
        }

        [TestMethod]
        public void InsensitiveTokens_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsNullWithNonWildcardPath_SingleNullIsReturned()
        {
            var tokens = JTokenExtensions.InsensitiveTokens(null, "path").ToList();
            Assert.AreEqual(1, tokens.Count);
            Assert.IsNull(tokens[0]);
        }

        [TestMethod]
        public void InsensitiveTokens_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsNullWithWildcardPath_EmptyIsReturned()
        {
            var tokens = JTokenExtensions.InsensitiveTokens(null, "some.*.path").ToList();
            Assert.AreEqual(0, tokens.Count);
        }

        /// <summary>
        /// Test data for tests that verify paths with wildcards, where potentially multiple tokens could be returned.
        /// Index 1: The path to test.
        /// Index 2: A list of object arrays, each array being the literal child elements to an expected token that will be matched from the path being tested.
        ///          A list element containing an empty array, followed by more elements of non-empty arrays,
        ///          denotes literal child elements to an expected token returned with LastValid behavior.
        /// Index 3: The display name of the test.
        /// </summary>
        public static IReadOnlyList<object[]> WildcardTestScenarios { get; } = new List<object[]>
        {
            new object[] {
                "topLevel1.subLevel2.*",
                new List<object[]> {
                    new object[] { "topLevel1", "subLevel2", "someNode" },
                    new object[] { "topLevel1", "subLevel2", "otherNode" },
                }, "Wildcard at end of path, no array in children, all children returned" },
            new object[] {
                "topLevel2.*",
                new List<object[]> {
                    new object[] { "topLevel2", "subLevel1" },
                    new object[] { "topLevel2", "subLevel2" },
                    new object[] { "topLevel2", "wildcardArray", 0 },
                    new object[] { "topLevel2", "wildcardArray", 1 },
                }, "Wildcard at end of path, array contained in children, all children and array elements returned" },
            new object[] {
                "topLevel1.*.otherNode",
                new List<object[]> {
                    new object[] { "topLevel1", "subLevel2", "otherNode" },
                    new object[] { }, // Below are expected paths for LastValid
                    new object[] { "topLevel1", "subLevel1" },
                }, "Wildcard in middle of path, path after wildcard only matches on property, that single property returned" },
            new object[] {
                "topLevel2.*.node2.someProp",
                new List<object[]> {
                    new object[] { "topLevel2", "subLevel2", "node2", "someProp" },
                    new object[] { "topLevel2", "wildcardArray", 1, "node2", "someProp" },
                    new object[] { }, // Below are expected paths for LastValid
                    new object[] { "topLevel2", "wildcardArray", 0 },
                    new object[] { "topLevel2", "subLevel1" },
                }, "Wildcard in middle of path, path after wildcard matches multiple properties including array elements, all matching properties returned" },
            new object[] {
                "topLevel1.subLevel1.intArray[*]",
                new List<object[]> {
                    new object[] { "topLevel1", "subLevel1", "intArray", 0 },
                    new object[] { "topLevel1", "subLevel1", "intArray", 1 },
                    new object[] { "topLevel1", "subLevel1", "intArray", 2 },
                }, "Wildcard array index, all array elements returned" },
            new object[] {
                "topLevel2.subLevel2[*]",
                new List<object[]> {
                    new object[] { }, // Below are expected paths for LastValid
                    new object[] { "topLevel2", "subLevel2" },
                }, "Wildcard array index, indexed property is not an array, nothing is matched" },
            new object[] {
                "topLevel1.*.*",
                new List<object[]> {
                    new object[] { "topLevel1", "subLevel1", "intArray", 0 },
                    new object[] { "topLevel1", "subLevel1", "intArray", 1 },
                    new object[] { "topLevel1", "subLevel1", "intArray", 2 },
                    new object[] { "topLevel1", "subLevel2", "someNode" },
                    new object[] { "topLevel1", "subLevel2", "otherNode" },
                }, "Multiple wildcards in path, all matching properties returned" },
            new object[] {
                "topLevel1.*.intArray[*]",
                new List<object[]> {
                    new object[] { "topLevel1", "subLevel1", "intArray", 0 },
                    new object[] { "topLevel1", "subLevel1", "intArray", 1 },
                    new object[] { "topLevel1", "subLevel1", "intArray", 2 },
                    new object[] { }, // Below are expected paths for LastValid
                    new object[] { "topLevel1", "subLevel2" },
                }, "Wildcard as a path property and array index, all matching properties returned" },
            new object[] {
                "*.subLevel2",
                new List<object[]> {
                    new object[] { "topLevel1", "subLevel2" },
                    new object[] { "topLevel2", "subLevel2" },
                    new object[] { }, // Below are expected paths for LastValid
                    new object[] { "topLevel3" },
                }, "Wildcard as first path property, all matching properties returned" },
        }.AsReadOnly();

        [DataTestMethod]
        [DynamicData(nameof(WildcardTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveTokens_WildcardPathWithNullBehavior_CorrectChildrenReturned(string path, List<object[]> expectedTokenPaths, string _)
        {
            var tokens = testJObject.InsensitiveTokens(path).ToList();

            // Iterate through expected token paths.
            // If an expected path is found with no elements, this signals the end of
            // fully resolved tokens. The rest lead to tokens which are expected to not
            // fully match, and are only returned with LastValid behavior.
            int expectedTokenCount = 0;
            foreach (var expectedPath in expectedTokenPaths)
            {
                if (expectedPath.Length == 0) break;
                
                expectedTokenCount++;

                // Retrieve expected token using explicit child elements
                JToken expectedToken = testJObject;
                foreach (var property in expectedPath)
                {
                    expectedToken = expectedToken[property];
                }
                
                // Verify token
                var result = tokens.Find(t => t == expectedToken);
                Assert.AreEqual(expectedToken, result);
            }

            // Verify the number of expected token paths matches the number returned.
            Assert.AreEqual(expectedTokenCount, tokens.Count);
        }

        [DataTestMethod]
        [DynamicData(nameof(WildcardTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveTokens_WildcardPathWithLastValidBehavior_CorrectChildrenReturned(string path, List<object[]> expectedTokenPaths, string _)
        {
            var tokens = testJObject.InsensitiveTokens(path, InsensitivePathNotFoundBehavior.LastValid).ToList();

            // Iterate through expected token paths.
            // If an expected path is found with no elements, this signals the end of
            // fully resolved tokens. The rest lead to tokens which are expected to not
            // fully match, and are only returned with LastValid behavior.
            int expectedTokenCount = 0;
            foreach (var expectedPath in expectedTokenPaths)
            {
                if (expectedPath.Length == 0)
                {
                    // The rest of the expected paths after this one
                    // should not match the full requested path. (LastValid behavior)
                    continue;
                }

                expectedTokenCount++;

                // Retrieve expected token using explicit child elements
                JToken expectedToken = testJObject;
                foreach (var property in expectedPath)
                {
                    expectedToken = expectedToken[property];
                }
                
                // Verify token
                var result = tokens.Find(t => t == expectedToken);
                Assert.AreEqual(expectedToken, result);
            }

            // Verify the number of expected token paths matches the number returned.
            Assert.AreEqual(expectedTokenCount, tokens.Count);
        }

        [DataTestMethod]
        [DynamicData(nameof(WildcardTestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void InsensitiveTokens_WildcardPathWithErrorBehavior_CorrectChildrenReturnedOrExceptionIsThrown(string path, List<object[]> expectedTokenPaths, string _)
        {
            // See if there are any branches in the JSON where a token won't be found.
            // expectedTokenPaths will have one element which is an empty array if so.
            bool expectException = expectedTokenPaths.Exists(path => path.Length == 0);

            try
            {
                testJObject.InsensitiveTokens(path, InsensitivePathNotFoundBehavior.Error).ToList();
                Assert.IsFalse(expectException);
            }
            catch
            {
                Assert.IsTrue(expectException);
            }
        }
    }
}
