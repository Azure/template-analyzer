// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
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

        private static readonly TemplateContext templateContext = new TemplateContext
        {
            OriginalTemplate = JObject.Parse(
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""parameters"": {
                    ""parameter1"": {
                        ""type"": ""int"",
                        ""minValue"": 0
                    }
                },
                ""resources"": [
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource0"",
                        ""name"": ""resource0"",
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource1"",
                        ""name"": ""resource1"",
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
                        ""type"": ""Microsoft.ResourceProvider/resource2"",
                        ""name"": ""resource2"",
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource3"",
                        ""name"": ""resource3"",
                        ""dependsOn"": [ ""resource2"" ],
                        ""properties"": {
                            ""somePath"": ""someValue""
                        },
                        ""resources"": [
                            {
                                ""type"": ""Microsoft.ResourceProvider/resource3-0"",
                                ""name"": ""resource3-0"",
                                ""properties"": {
                                    ""somePath"": ""someValue""
                                }
                            }
                        ]
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource4"",
                        ""name"": ""resource4"",
                        ""dependsOn"": [ ""resource2"" ],
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
                        ""type"": ""int"",
                        ""minValue"": 0
                    }
                },
                ""resources"": [
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource0"",
                        ""name"": ""resource0"",
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource2"",
                        ""name"": ""resource2"",
                        ""properties"": {
                            ""somePath"": ""someValue""
                        },
                        ""resources"": [
                            {
                                ""type"": ""Microsoft.ResourceProvider/resource3"",
                                ""name"": ""resource3"",
                                ""dependsOn"": [ ""resource2"" ],
                                ""properties"": {
                                    ""somePath"": ""someValue""
                                },
                                ""resources"": [
                                    {
                                        ""type"": ""Microsoft.ResourceProvider/resource3-0"",
                                        ""name"": ""resource3-0"",
                                        ""properties"": {
                                            ""somePath"": ""someValue""
                                        }
                                    },
                                    {
                                        ""type"": ""Microsoft.ResourceProvider/resource4"",
                                        ""name"": ""resource4"",
                                        ""dependsOn"": [ ""resource3"" ],
                                        ""properties"": {
                                            ""somePath"": ""someValue""
                                        }
                                    }
                                ]
                            }
                        ]
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource3"",
                        ""name"": ""resource3"",
                        ""dependsOn"": [ ""resource2"" ],
                        ""properties"": {
                            ""somePath"": ""someValue""
                        },
                        ""resources"": [
                            {
                                ""type"": ""Microsoft.ResourceProvider/resource3-0"",
                                ""name"": ""resource3-0"",
                                ""properties"": {
                                    ""somePath"": ""someValue""
                                }
                            },
                            {
                                ""type"": ""Microsoft.ResourceProvider/resource4"",
                                ""name"": ""resource4"",
                                ""dependsOn"": [ ""resource3"" ],
                                ""properties"": {
                                    ""somePath"": ""someValue""
                                }
                            }
                        ]
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource4"",
                        ""name"": ""resource4"",
                        ""dependsOn"": [ ""resource3"" ],
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    },
                    {
                        ""type"": ""Microsoft.ResourceProvider/resource1"",
                        ""name"": ""resource1.0"",
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
                        ""name"": ""resource1.1"",
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
            }"),

            ResourceMappings = new Dictionary<string, string>
            {
                { "resources[0]", "resources[0]" },
                { "resources[1]", "resources[2]" },
                { "resources[1].resources[0]", "resources[3]" },
                { "resources[1].resources[0].resources[0]", "resources[3].resources[0]" },
                { "resources[1].resources[0].resources[1]", "resources[4]" },
                { "resources[2]", "resources[3]" },
                { "resources[2].resources[0]", "resources[3].resources[0]" },
                { "resources[2].resources[1]", "resources[4]" },
                { "resources[3]", "resources[4]" },
                { "resources[4]", "resources[1]" },
                { "resources[5]", "resources[1]" }
            }
        };

        #endregion

        public static IReadOnlyList<object[]> TestScenarios { get; } = new List<object[]>
        {
            // Test data for test ResolveSourceLocation_ReturnsCorrectLineNumber.
            // Index one is for parameter 'path'.
            // Index two (a sub-array) is for parameter 'pathInOrginalTemplate'.
            // Index three is the test display name.  This is just so GetDisplayName() can do a lookup and is not used in the test.
            new object[] { "resources[0].properties.somePath", new object[] { "resources", 0, "properties", "somePath" }, "Path matches original template exactly" },
            new object[] { "parameters.parameter1.maxValue", new object[] { "parameters", "parameter1" }, "Beginning of path matches original template parameters, but has missing property" },
            new object[] { "resources[0].anExpandedProperty", new object[] { "resources", 0 }, "Beginning of path matches original template resources array, but has missing property" },
            new object[] { "resources[4].properties.anotherProperty", new object[] { "resources", 1, "properties", "anotherProperty" }, "Path is in a moved (original from copy loop) resource" },
            new object[] { "resources[5].properties.anotherProperty", new object[] { "resources", 1, "properties", "anotherProperty" }, "Path is in a new (copied/duplicated) resource" },
            new object[] { "resources[5].properties.missingProperty", new object[] { "resources", 1, "properties" }, "Path is in a new/duplicated resource and has missing property" },
            new object[] { "resources[4].properties.missingProperty", new object[] { "resources", 1, "properties" }, "Path is in a reordered resource from copying and has missing property" },
            new object[] { "resources[0].resources[0].someProperty", new object[] { }, "Path goes to a resource that's been copied into another resource from expansion, but resource does not exist" },
            new object[] { "resources[1].resources[0].properties.somePath", new object[] { "resources", 3, "properties", "somePath" }, "Path goes to a resource that's been copied into another resource that was also copied in expansion" },
            new object[] { "resources[1].resources[0].resources[1].properties.somePath", new object[] { "resources", 4, "properties", "somePath" }, "Path goes to a resource that's been copied into another resource as a grandchild" },
            new object[] { "resources[1].resources[0].resources[0].properties.somePath", new object[] { "resources", 3, "resources", 0, "properties", "somePath" }, "Path goes to a resource that's been copied but was still originally a child resource" },
            new object[] { "resources[1].resources[0].dependsOn[1]", new object[] { "resources", 3, "dependsOn" }, "Path goes to a property not specified in original template in a copied resource that is also an array" },
            new object[] { "resources[2].dependsOn[1]", new object[] { "resources", 3, "dependsOn" }, "Path goes to a property not specified in original template in a non-copied resource that is also an array" }
        }.AsReadOnly();

        // Just returns the element in the last index of the array from TestScenarios
        public static string GetDisplayName(MethodInfo _, object[] data) => (string)data[^1];

        [DataTestMethod]
        [DynamicData(nameof(TestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void ResolveSourceLocation_ReturnsCorrectLineNumber(string path, object[] pathInOrginalTemplate, string _)
        {
            // Resolve line number
            var resolvedLineNumber = new JsonSourceLocationResolver(templateContext)
                .ResolveSourceLocation(path).LineNumber;

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
        [DataRow("resources[0].type", 11, DisplayName = "")]
        [DataRow("resources[0].name", 12, DisplayName = "")]
        [DataRow("resources[0].properties.somePath", 14, DisplayName = "")]
        public void ResolveLineNumber_ContextWithOffset_ReturnsCorrectLineNumber(string path, int lineNumber)
        {
            TemplateContext templateContextWithOffset = new()
            {
                OriginalTemplate = JObject.Parse(
                @"{
                            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                            ""parameters"": {
                                ""parameter1"": {
                                    ""type"": ""int"",
                                    ""minValue"": 0
                                }
                            },
                            ""resources"": [
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource0"",
                                    ""name"": ""resource0"",
                                    ""properties"": {
                                        ""somePath"": ""someValue""
                                    }
                                },
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource1"",
                                    ""name"": ""resource1"",
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
                                    ""type"": ""Microsoft.ResourceProvider/resource2"",
                                    ""name"": ""resource2"",
                                    ""properties"": {
                                        ""somePath"": ""someValue""
                                    }
                                },
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource3"",
                                    ""name"": ""resource3"",
                                    ""dependsOn"": [ ""resource2"" ],
                                    ""properties"": {
                                        ""somePath"": ""someValue""
                                    },
                                    ""resources"": [
                                        {
                                            ""type"": ""Microsoft.ResourceProvider/resource3-0"",
                                            ""name"": ""resource3-0"",
                                            ""properties"": {
                                                ""somePath"": ""someValue""
                                            }
                                        }
                                    ]
                                },
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource4"",
                                    ""name"": ""resource4"",
                                    ""dependsOn"": [ ""resource2"" ],
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
                                    ""type"": ""int"",
                                    ""minValue"": 0
                                }
                            },
                            ""resources"": [
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource0"",
                                    ""name"": ""resource0"",
                                    ""properties"": {
                                        ""somePath"": ""someValue""
                                    }
                                },
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource2"",
                                    ""name"": ""resource2"",
                                    ""properties"": {
                                        ""somePath"": ""someValue""
                                    },
                                    ""resources"": [
                                        {
                                            ""type"": ""Microsoft.ResourceProvider/resource3"",
                                            ""name"": ""resource3"",
                                            ""dependsOn"": [ ""resource2"" ],
                                            ""properties"": {
                                                ""somePath"": ""someValue""
                                            },
                                            ""resources"": [
                                                {
                                                    ""type"": ""Microsoft.ResourceProvider/resource3-0"",
                                                    ""name"": ""resource3-0"",
                                                    ""properties"": {
                                                        ""somePath"": ""someValue""
                                                    }
                                                },
                                                {
                                                    ""type"": ""Microsoft.ResourceProvider/resource4"",
                                                    ""name"": ""resource4"",
                                                    ""dependsOn"": [ ""resource3"" ],
                                                    ""properties"": {
                                                        ""somePath"": ""someValue""
                                                    }
                                                }
                                            ]
                                        }
                                    ]
                                },
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource3"",
                                    ""name"": ""resource3"",
                                    ""dependsOn"": [ ""resource2"" ],
                                    ""properties"": {
                                        ""somePath"": ""someValue""
                                    },
                                    ""resources"": [
                                        {
                                            ""type"": ""Microsoft.ResourceProvider/resource3-0"",
                                            ""name"": ""resource3-0"",
                                            ""properties"": {
                                                ""somePath"": ""someValue""
                                            }
                                        },
                                        {
                                            ""type"": ""Microsoft.ResourceProvider/resource4"",
                                            ""name"": ""resource4"",
                                            ""dependsOn"": [ ""resource3"" ],
                                            ""properties"": {
                                                ""somePath"": ""someValue""
                                            }
                                        }
                                    ]
                                },
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource4"",
                                    ""name"": ""resource4"",
                                    ""dependsOn"": [ ""resource3"" ],
                                    ""properties"": {
                                        ""somePath"": ""someValue""
                                    }
                                },
                                {
                                    ""type"": ""Microsoft.ResourceProvider/resource1"",
                                    ""name"": ""resource1.0"",
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
                                    ""name"": ""resource1.1"",
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
                        }"),

                ResourceMappings = new Dictionary<string, string>
                        {
                            { "resources[0]", "resources[0]" },
                            { "resources[1]", "resources[2]" },
                            { "resources[1].resources[0]", "resources[3]" },
                            { "resources[1].resources[0].resources[0]", "resources[3].resources[0]" },
                            { "resources[1].resources[0].resources[1]", "resources[4]" },
                            { "resources[2]", "resources[3]" },
                            { "resources[2].resources[0]", "resources[3].resources[0]" },
                            { "resources[2].resources[1]", "resources[4]" },
                            { "resources[3]", "resources[4]" },
                            { "resources[4]", "resources[1]" },
                            { "resources[5]", "resources[1]" }
                        },
                Offset = 5
            };


            // Resolve line number with offset in context set
            var resolvedLineNumberWithOffset = new JsonSourceLocationResolver(templateContextWithOffset)
                .ResolveSourceLocation(path).LineNumber;

            // Resolve line number with offset in context not set
            var resolvedLineNumberWithoutOffset = new JsonSourceLocationResolver(templateContext)
                .ResolveSourceLocation(path).LineNumber;

            int expectedLineNumberWithOffset = lineNumber + templateContextWithOffset.Offset; // LineNumber in template plus offset
            int expectedLineNumberWithoutOffset = lineNumber + templateContext.Offset;

            Assert.AreEqual(expectedLineNumberWithOffset, resolvedLineNumberWithOffset);
            Assert.AreEqual(expectedLineNumberWithoutOffset, resolvedLineNumberWithoutOffset);
        }

        [DataTestMethod]
        [DataRow("MissingFirstChild.any.other.path", DisplayName = "First child in path not found")]
        [DataRow("resources[7].type", DisplayName = "Extra resource")]
        [DataRow("resources[6].type", DisplayName = "Extra copied resource with missing source copy loop")]
        public void ResolveSourceLocation_UnableToFindEquivalentLocationInOriginal_Returns1(string path)
        {
            Assert.AreEqual(
                1,
                new JsonSourceLocationResolver(templateContext)
                .ResolveSourceLocation(path).LineNumber);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveSourceLocation_PathIsNull_ThrowsException()
        {
            new JsonSourceLocationResolver(templateContext)
                .ResolveSourceLocation(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveSourceLocation_OriginalTemplateIsNull_ThrowsException()
        {
            new JsonSourceLocationResolver(
                new TemplateContext
                {
                    OriginalTemplate = null,
                    ExpandedTemplate = templateContext.ExpandedTemplate
                })
                .ResolveSourceLocation("path");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveSourceLocation_ExpandedTemplateIsNullAndPathContainsResourcesArray_ThrowsException()
        {
            new JsonSourceLocationResolver(
                new TemplateContext
                {
                    OriginalTemplate = templateContext.OriginalTemplate,
                    ExpandedTemplate = null
                })
                .ResolveSourceLocation("resources[4]");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveSourceLocation_ExpandedTemplateIsNullAndPathContainsChildResourcesArray_ThrowsException()
        {
            new JsonSourceLocationResolver(
                new TemplateContext
                {
                    OriginalTemplate = templateContext.OriginalTemplate,
                    ExpandedTemplate = null
                })
                .ResolveSourceLocation("resources[1].resources[0]");
        }

        [TestMethod]
        public void ResolveSourceLocation_ExpandedTemplateIsNullAndPathDoesNotContainResourcesArray_ReturnsLineNumber()
        {
            new JsonSourceLocationResolver(
                new TemplateContext
                {
                    OriginalTemplate = templateContext.OriginalTemplate,
                    ExpandedTemplate = null
                })
                .ResolveSourceLocation("parameters.missingParameter");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullTemplateContext_ThrowsException()
        {
            new JsonSourceLocationResolver(null);
        }
    }
}
