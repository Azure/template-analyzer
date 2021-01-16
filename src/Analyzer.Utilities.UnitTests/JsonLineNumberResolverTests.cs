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
        public static List<object[]> TestScenarios { get; } = new List<object[]>
        {
            new object[] { "resources[0].properties.somePath", new object[] { "resources", 0, "properties", "somePath" }, "Scenario 1 - path matches both templates exactly" },
            new object[] { "parameters.parameter1.maxValue", new object[] { "parameters", "parameter1" }, "Scenario 2 - beginning of path matches original template, but has missing property" },
            new object[] { "resources[\"stringIndex\"]", new object[] { "resources" }, "Scenario 2 (altered) - resources does not use integer index into array" },
            new object[] { "resources[2].properties.anotherProperty", new object[] { "resources", 0, "properties", "anotherProperty" }, "Scenario 3 - path is in copied resource" },
            new object[] { "resources[2].properties.missingProperty", new object[] { "resources", 0, "properties" }, "Scenario 3 & 2 - path is in copied resource and has missing property" },
            new object[] { "resources[0].resources[0].someProperty", new object[] { }, "Scenario 4 - a resource is copied into another resource from expansion" },
            new object[] { "resources[2].resources[0].someProperty", new object[] { }, "Scenario 3 & 4 - a resource is copied into another resource copy from expansion" }
        };

        public static string GenerateDisplayName(MethodInfo methodInfo, object[] data)
            => (string)data[^1];

        [DataTestMethod]
        [DynamicData(nameof(TestScenarios), DynamicDataDisplayName = nameof(GenerateDisplayName))]
        public void ResolveLineNumberForOriginalTemplate_ReturnsCorrectLineNumber(string path, object[] pathInOrginalTemplate, string displayNameNotUsed)
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
        public void ResolveLineNumberForOriginalTemplate_ExpandedTemplateIsNullForScenario3_ThrowsException()
        {
            new JsonLineNumberResolver()
                .ResolveLineNumberForOriginalTemplate(
                    "resources[2]",
                    null,
                    originalTemplate);
        }

        [TestMethod]
        public void ResolveLineNumberForOriginalTemplate_ExpandedTemplateIsNullForScenario1or2_ReturnsLineNumber()
        {
            new JsonLineNumberResolver()
                .ResolveLineNumberForOriginalTemplate(
                    "parameters.missingParameter",
                    null,
                    originalTemplate);
        }

        private JToken originalTemplate = JObject.Parse(
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

        private JToken expandedTemplate = JObject.Parse(
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
    }
}
