// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities.UnitTests
{
    [TestClass]
    public class JsonLineNumberResolverTests
    {
        #region Test Template Context

        private readonly TemplateContext templateContext = new TemplateContext
        {
            OriginalTemplate = JObject.Parse(
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""parameters"": {
                    ""parameter1"": {
                        ""type"": ""integer"",
                        ""minValue"": 0
                    }
                },
                ""resources"": [
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource0"",
                        ""properties"": {
                            ""somePath"": ""someValue"",
                            ""anotherProperty"": true
                        },
                        ""copy"": {
                            ""name"": ""copyLoop"",
                            ""count"": 2
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource1"",
                        ""name"": ""resource1"",
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource2"",
                        ""name"": ""resource2"",
                        ""dependsOn"": [ ""resource1"" ],
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    }
                ]
            }"),

            ExpandedTemplate = JObject.Parse(
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""parameters"": {
                    ""parameter1"": {
                        ""type"": ""integer"",
                        ""minValue"": 0
                    }
                },
                ""resources"": [
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource0"",
                        ""properties"": {
                            ""somePath"": ""someValue"",
                            ""anotherProperty"": true
                        },
                        ""copy"": {
                            ""name"": ""copyLoop"",
                            ""count"": 2
                        },
                        ""anExpandedProperty"": ""anExpandedValue""
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource1"",
                        ""name"": ""resource1"",
                        ""properties"": {
                            ""somePath"": ""someValue""
                        },
                        ""resources"": [
                            {
                                ""type"": ""Microsoft.ResourceProvider/resource2"",
                                ""name"": ""resource2"",
                                ""dependsOn"": [ ""resource1"" ],
                                ""properties"": {
                                    ""somePath"": ""someValue""
                                }
                            }
                        ]
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource2"",
                        ""name"": ""resource2"",
                        ""dependsOn"": [ ""resource1"" ],
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource0"",
                        ""properties"": {
                            ""somePath"": ""someValue"",
                            ""anotherProperty"": true
                        },
                        ""copy"": {
                            ""name"": ""copyLoop"",
                            ""count"": 2
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/badResource"",
                        ""copy"": {
                            ""name"": ""missingSourceCopyInOriginal"",
                            ""count"": 2
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/ExtraResourceInExpandedTemplate"",
                    }
                ]
            }")
        };

        #endregion

        public static IReadOnlyList<object[]> TestScenarios { get; } = new List<object[]>
        {
            // Test data for test ResolveLineNumber_ReturnsCorrectLineNumber.
            // Index one is for parameter 'path'.
            // Index two (a sub-array) is for parameter 'pathInOrginalTemplate'.
            // Index three is the test display name.  This is just so GetDisplayName() can do a lookup and is not used in the test.
            new object[] { "resources[0].properties.somePath", new object[] { "resources", 0, "properties", "somePath" }, "Path matches original template exactly" },
            new object[] { "parameters.parameter1.maxValue", new object[] { "parameters", "parameter1" }, "Beginning of path matches original template parameters, but has missing property" },
            new object[] { "resources[0].anExpandedProperty", new object[] { "resources", 0 }, "Beginning of path matches original template resources array, but has missing property" },
            new object[] { "resources[3].properties.anotherProperty", new object[] { "resources", 0, "properties", "anotherProperty" }, "Path is in a copied resource" },
            new object[] { "resources[3].properties.missingProperty", new object[] { "resources", 0, "properties" }, "Path is in a copied resource and has missing property" },
            new object[] { "resources[0].resources[0].someProperty", new object[] { }, "Path goes to a resource that's been copied into another resource from expansion, but resource does not exist" },
            new object[] { "resources[1].resources[0].properties.somePath", new object[] { "resources", 2, "properties", "somePath" }, "Path goes to a resource that's been copied into another resource that was also copied in expansion" }
        }.AsReadOnly();

        // Just returns the element in the last index of the array from TestScenarios
        public static string GetDisplayName(MethodInfo _, object[] data) => (string)data[^1];

        [DataTestMethod]
        [DynamicData(nameof(TestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void ResolveLineNumber_ReturnsCorrectLineNumber(string path, object[] pathInOrginalTemplate, string _)
        {
            // Resolve line number
            var resolvedLineNumber = new JsonLineNumberResolver(templateContext)
                .ResolveLineNumber(path);

            // Get expected line number
            var tokenInOriginalTemplate = templateContext.OriginalTemplate;
            foreach (var pathSegment in pathInOrginalTemplate)
            {
                tokenInOriginalTemplate = tokenInOriginalTemplate[pathSegment];
            }

            var expectedLineNumber = (tokenInOriginalTemplate as IJsonLineInfo).LineNumber;
            Assert.AreEqual(expectedLineNumber, resolvedLineNumber);
        }

        [DataTestMethod]
        [DataRow("MissingFirstChild.any.other.path", DisplayName = "First child in path not found")]
        [DataRow("resources[5].type", DisplayName = "Extra resource")]
        [DataRow("resources[4].type", DisplayName = "Extra copied resource with missing source copy loop")]
        public void ResolveLineNumber_UnableToFindEquivalentLocationInOriginal_Returns0(string path)
        {
            Assert.AreEqual(
                0,
                new JsonLineNumberResolver(templateContext)
                .ResolveLineNumber(path));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveLineNumber_PathIsNull_ThrowsException()
        {
            new JsonLineNumberResolver(templateContext)
                .ResolveLineNumber(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveLineNumber_OriginalTemplateIsNull_ThrowsException()
        {
            new JsonLineNumberResolver(
                new TemplateContext
                {
                    OriginalTemplate = null,
                    ExpandedTemplate = templateContext.ExpandedTemplate
                })
                .ResolveLineNumber("path");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveLineNumber_ExpandedTemplateIsNullAndPathContainsResourcesArray_ThrowsException()
        {
            new JsonLineNumberResolver(
                new TemplateContext
                {
                    OriginalTemplate = templateContext.OriginalTemplate,
                    ExpandedTemplate = null
                })
                .ResolveLineNumber("resources[3]");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveLineNumber_ExpandedTemplateIsNullAndPathContainsChildResourcesArray_ThrowsException()
        {
            new JsonLineNumberResolver(
                new TemplateContext
                {
                    OriginalTemplate = templateContext.OriginalTemplate,
                    ExpandedTemplate = null
                })
                .ResolveLineNumber("resources[1].resources[0]");
        }

        [TestMethod]
        public void ResolveLineNumber_ExpandedTemplateIsNullAndPathDoesNotContainResourcesArray_ReturnsLineNumber()
        {
            new JsonLineNumberResolver(
                new TemplateContext
                {
                    OriginalTemplate = templateContext.OriginalTemplate,
                    ExpandedTemplate = null
                })
                .ResolveLineNumber("parameters.missingParameter");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullTemplateContext_ThrowsException()
        {
            new JsonLineNumberResolver(null);
        }
    }
}
