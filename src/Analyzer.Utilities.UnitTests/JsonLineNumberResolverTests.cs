// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
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
            // Test data for test ResolveLineNumber_ReturnsCorrectLineNumber.
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
        [DataRow("resources[7].type", DisplayName = "Extra resource")]
        [DataRow("resources[6].type", DisplayName = "Extra copied resource with missing source copy loop")]
        public void ResolveLineNumber_UnableToFindEquivalentLocationInOriginal_Returns1(string path)
        {
            Assert.AreEqual(
                1,
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
                .ResolveLineNumber("resources[4]");
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

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ResolveLineNumber_NullRootTemplateContext_ThrowsException()
        {
            new JsonLineNumberResolver(
                new TemplateContext
                {
                    OriginalTemplate = templateContext.OriginalTemplate,
                    ExpandedTemplate = templateContext.ExpandedTemplate,
                    IsMainTemplate = false,
                    ParentContext = null
                })
                .ResolveLineNumber("path");
        }

        [DataTestMethod]
        [DataRow("resources", 0, new object[] { "resources" }, DisplayName = "Parent template resources property found")]
        [DataRow("resources[0].properties.template", 0, new object[] { "resources", 0, "properties", "template"}, DisplayName = "Parent template resource property found")]
        [DataRow("resources[0].properties.siteConfig.ftpsState", 1, new object[] { "resources", 0, "properties", "template", "resources", 0, "properties", "siteConfig", "ftpsState" }, DisplayName = "One level nested template resource property found")]
        [DataRow("parameters.ftpsState.defaultValue", 1, new object[] { "resources", 0, "properties", "template", "parameters", "ftpsState", "defaultValue" }, DisplayName = "One level nested template parameter found")]
        [DataRow("resources[0].resources[0].properties.template", 1, new object[] { "resources", 0, "properties", "template", "resources", 1, "properties", "template" }, DisplayName = "Original resource found, from a resource dependant")]
        [DataRow("resources[0].resourceGroup", 2, new object[] { "resources", 0, "properties", "template", "resources", 1, "properties", "template", "resources", 0, "resourceGroup" }, DisplayName = "Two levels nested template resource property found")]
        [DataRow("resources[0].properties.expressionEvaluationOptions", 2, new object[] { "resources", 0, "properties", "template", "resources", 1, "properties", "template", "resources", 0, "properties", "expressionEvaluationOptions" }, DisplayName = "Two levels nested template resource property found")]
        [DataRow("resources[1].properties", 2, new object[] { "resources", 0, "properties", "template", "resources", 1, "properties", "template", "resources", 0, "properties" }, DisplayName = "Original resource found, from a nested template resource copy")]
        [DataRow("contentVersion", 3, new object[] { "resources", 0, "properties", "template", "resources", 1, "properties", "template", "resources", 0, "properties", "template", "contentVersion" }, DisplayName = "Three levels nested template property found")]
        [DataRow("resources[0].location", 3, new object[] { "resources", 0, "properties", "template", "resources", 1, "properties", "template", "resources", 0, "properties", "template", "resources", 0, "location" }, DisplayName = "Three levels nested template resource property found")]
        [DataRow("resources[2].properties", 3, new object[] { "resources", 0, "properties", "template", "resources", 1, "properties", "template", "resources", 0, "properties", "template", "resources", 0, "properties" }, DisplayName = "Original resource found, from a resource copy")]
        public void ResolveLineNumber_TemplatesWithNestedTemplates_ReturnsCorrectLineNumber(string pathInTheExpandedTemplate, int numberOfTemplate, object[] pathInTheOriginalTemplate)
        {
            string originalThirdChildTemplate =
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""resources"": [
                    {
                        ""apiVersion"": ""2019-08-01"",
                        ""type"": ""Microsoft.Web/sites"",
                        ""kind"": ""api"",
                        ""name"": ""[concat(copyIndex(),'_aResourceToFlag')]"",
                        ""location"": ""US"",
                        ""copy"": {
                            ""count"": 3,
                            ""name"": ""aCopyLoop""
                        },
                        ""properties"": {}
                    }
                ]
            }";
            var templateProcessorThirdChild = new ArmTemplateProcessor(originalThirdChildTemplate);
            var expandedThirdChildTemplate = templateProcessorThirdChild.ProcessTemplate();

            string templateEnding = @"
                        }
                    }
                ]
            }";

            string originalSecondChildTemplate =
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""resources"": [
                    {
                        ""type"": ""Microsoft.Resources/deployments"",
                        ""apiVersion"": ""2016-09-01"",
                        ""copy"": {
                            ""count"": 2,
                            ""name"": ""anotherCopyLoop""
                        },
                        ""name"": ""[concat(copyIndex(),'_nestedTemplate3')]"",
                        ""resourceGroup"": ""aResourceGroup"",
                        ""properties"": {
                            ""mode"": ""Incremental"",
                            ""expressionEvaluationOptions"": {
                                ""scope"": ""inner""
                            },
                            ""template"": " + originalThirdChildTemplate + templateEnding;
            var templateProcessorSecondChild = new ArmTemplateProcessor(originalSecondChildTemplate);
            var expandedSecondChildTemplate = templateProcessorSecondChild.ProcessTemplate();

            string originalFirstChildTemplate =
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""parameters"": {
                    ""ftpsState"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""undesiredValue""
                    }
                },
                ""resources"": [
                    {
                        ""apiVersion"": ""2019-08-01"",
                        ""type"": ""Microsoft.Web/sites"",
                        ""kind"": ""api"",
                        ""name"": ""anotherResourceToFlag"",
                        ""location"": ""US"",
                        ""properties"": {
                            ""siteConfig"": {
                                ""ftpsState"": ""[parameters('ftpsState')]""
                            }
                        }
                    }, 
                    {
                        ""type"": ""Microsoft.Resources/deployments"",
                        ""apiVersion"": ""2016-09-01"",
                        ""name"": ""nestedTemplate2"",
                        ""resourceGroup"": ""aResourceGroup"",
                        ""dependsOn"": [
                            ""anotherResourceToFlag""
                        ],
                        ""properties"": {
                            ""mode"": ""Incremental"",
                            ""expressionEvaluationOptions"": {
                                ""scope"": ""inner""
                            },
                            ""template"": " + originalSecondChildTemplate + templateEnding;
            var templateProcessorFirstChild = new ArmTemplateProcessor(originalFirstChildTemplate);
            var expandedFirstChildTemplate = templateProcessorFirstChild.ProcessTemplate();

            string originalParentTemplate =
            @"{
                ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
                ""contentVersion"": ""1.0.0.0"",
                ""resources"": [
                    {
                        ""type"": ""Microsoft.Resources/deployments"",
                        ""apiVersion"": ""2019-10-01"",
                        ""name"": ""nestedTemplate"",
                        ""properties"": {
                            ""mode"": ""Incremental"",
                            ""expressionEvaluationOptions"": {
                                ""scope"": ""inner""
                            },
                            ""template"": " + originalFirstChildTemplate + templateEnding;
            var templateProcessorOriginalParent = new ArmTemplateProcessor(originalParentTemplate);
            var expandedParentTemplate = templateProcessorOriginalParent.ProcessTemplate();

            var parentTemplateContext = new TemplateContext
            {
                OriginalTemplate = JObject.Parse(originalParentTemplate),
                ExpandedTemplate = expandedParentTemplate,
                ResourceMappings = templateProcessorOriginalParent.ResourceMappings
            };

            var firstChildTemplateContext = new TemplateContext
            {
                OriginalTemplate = JObject.Parse(originalFirstChildTemplate),
                ExpandedTemplate = expandedFirstChildTemplate,
                ResourceMappings = templateProcessorFirstChild.ResourceMappings,
                IsMainTemplate = false,
                PathPrefix = "resources[0].properties.template",
                ParentContext = parentTemplateContext
            };

            var secondChildTemplateContext = new TemplateContext
            {
                OriginalTemplate = JObject.Parse(originalSecondChildTemplate),
                ExpandedTemplate = expandedSecondChildTemplate,
                ResourceMappings = templateProcessorSecondChild.ResourceMappings,
                IsMainTemplate = false,
                PathPrefix = "resources[1].properties.template",
                ParentContext = firstChildTemplateContext
            };

            var thirdChildTemplateContext = new TemplateContext
            {
                OriginalTemplate = JObject.Parse(originalThirdChildTemplate),
                ExpandedTemplate = expandedThirdChildTemplate,
                ResourceMappings = templateProcessorThirdChild.ResourceMappings,
                IsMainTemplate = false,
                PathPrefix = "resources[0].properties.template",
                ParentContext = secondChildTemplateContext
            };

            TemplateContext[] templateContexts = { parentTemplateContext, firstChildTemplateContext, secondChildTemplateContext, thirdChildTemplateContext };

            // Resolve line number
            var currentTemplateContext = templateContexts[numberOfTemplate];
            var resolvedLineNumber = new JsonLineNumberResolver(currentTemplateContext).ResolveLineNumber(pathInTheExpandedTemplate);

            // Get expected line number
            var tokenInOriginalTemplate = parentTemplateContext.OriginalTemplate;
            foreach (var pathSegment in pathInTheOriginalTemplate)
            {
                tokenInOriginalTemplate = tokenInOriginalTemplate[pathSegment];
            }

            var expectedLineNumber = (tokenInOriginalTemplate as IJsonLineInfo).LineNumber;

            Assert.AreEqual(expectedLineNumber, resolvedLineNumber);
        }
    }
}