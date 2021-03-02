// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities.UnitTests
{
    [TestClass]
    public class JsonLineNumberResolverTests
    {
        #region Test Template

        private readonly JToken originalTemplate = JObject.Parse(
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
                        ""properties"": {
                            ""somePath"": ""someValue""
                        }
                    }
                ]
            }");

        private readonly JToken expandedTemplate = JObject.Parse(
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
            }");

        #endregion

        public static IReadOnlyList<object[]> TestScenarios { get; } = new List<object[]>
        {
            // Test data for test ResolveLineNumberForOriginalTemplate_ReturnsCorrectLineNumber.
            // Index one is for parameter 'path'.
            // Index two (a sub-array) is for parameter 'pathInOrginalTemplate'.
            // Index three is the test display name.  This is just so GetDisplayName() can do a lookup and is not used in the test.
            new object[] { "resources[0].properties.somePath", new object[] { "resources", 0, "properties", "somePath" }, "Path matches original template exactly" },
            new object[] { "parameters.parameter1.maxValue", new object[] { "parameters", "parameter1" }, "Beginning of path matches original template parameters, but has missing property" },
            new object[] { "resources[0].anExpandedProperty", new object[] { "resources", 0 }, "Beginning of path matches original template resources array, but has missing property" },
            new object[] { "resources[2].properties.anotherProperty", new object[] { "resources", 0, "properties", "anotherProperty" }, "Path is in a copied resource" },
            new object[] { "resources[2].properties.missingProperty", new object[] { "resources", 0, "properties" }, "Path is in a copied resource and has missing property" },
            new object[] { "resources[0].resources[0].someProperty", new object[] { }, "Path goes to a resource that's been copied into another resource from expansion (not implementd yet)" },
            new object[] { "resources[2].resources[0].someProperty", new object[] { }, "Path goes to a resource that's been copied into another resource that was also copied in expansion (not implementd yet)" }
        }.AsReadOnly();

        // Just returns the element in the last index of the array from TestScenarios
        public static string GetDisplayName(MethodInfo _, object[] data) => (string)data[^1];

        [DataTestMethod]
        [DynamicData(nameof(TestScenarios), DynamicDataDisplayName = nameof(GetDisplayName))]
        public void ResolveLineNumberForOriginalTemplate_ReturnsCorrectLineNumber(string path, object[] pathInOrginalTemplate, string _)
        {
            // Resolve line number
            var resolvedLineNumber = new JsonLineNumberResolver()
                .ResolveLineNumberForOriginalTemplate(
                    path,
                    expandedTemplate,
                    originalTemplate);

            // Get expected line number
            var tokenInOriginalTemplate = originalTemplate;
            foreach (var pathSegment in pathInOrginalTemplate)
            {
                tokenInOriginalTemplate = tokenInOriginalTemplate[pathSegment];
            }

            var expectedLineNumber = (tokenInOriginalTemplate as IJsonLineInfo).LineNumber;
            Assert.AreEqual(expectedLineNumber, resolvedLineNumber);
        }

        [DataTestMethod]
        [DataRow("MissingFirstChild.any.other.path", DisplayName = "First child in path not found")]
        [DataRow("resources[4].type", DisplayName = "Extra resource")]
        [DataRow("resources[3].type", DisplayName = "Extra copied resource with missing source copy loop")]
        public void ResolveLineNumberForOriginalTemplate_UnableToFindEquivalentLocationInOriginal_Returns0(string path)
        {
            Assert.AreEqual(
                0,
                new JsonLineNumberResolver()
                    .ResolveLineNumberForOriginalTemplate(
                        path,
                        expandedTemplate,
                        originalTemplate));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveLineNumberForOriginalTemplate_PathIsNull_ThrowsException()
        {
            new JsonLineNumberResolver()
                .ResolveLineNumberForOriginalTemplate(
                    null,
                    expandedTemplate,
                    originalTemplate);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveLineNumberForOriginalTemplate_OriginalTemplateIsNull_ThrowsException()
        {
            new JsonLineNumberResolver()
                .ResolveLineNumberForOriginalTemplate(
                    "path",
                    expandedTemplate,
                    null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ResolveLineNumberForOriginalTemplate_ExpandedTemplateIsNullAndPathContainsResourcesArray_ThrowsException()
        {
            new JsonLineNumberResolver()
                .ResolveLineNumberForOriginalTemplate(
                    "resources[2]",
                    null,
                    originalTemplate);
        }

        [TestMethod]
        public void ResolveLineNumberForOriginalTemplate_ExpandedTemplateIsNullAndPathDoesNotContainResourcesArray_ReturnsLineNumber()
        {
            new JsonLineNumberResolver()
                .ResolveLineNumberForOriginalTemplate(
                    "parameters.missingParameter",
                    null,
                    originalTemplate);
        }
    }
}
