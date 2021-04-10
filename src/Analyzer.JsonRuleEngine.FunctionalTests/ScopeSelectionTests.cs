// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.FunctionalTests
{
    [TestClass]
    public class ScopeSelectionTests
    {
        // This template is incomplete.  It is only for scope testing purposes.
        #region Mock Template
        string mockTemplate = @"
{
  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {
    ""location"": {
      ""type"": ""string"",
      ""defaultValue"": ""[resourceGroup().location]"",
      ""metadata"": {
        ""description"": ""Location for all resources.""
      }
    }
  },
  ""resources"": [
    {
      ""apiVersion"": ""2015-06-15"",
      ""type"": ""Microsoft.Network/virtualNetworks"",
      ""name"": ""myVnet"",
      ""location"": ""[parameters('location')]"",
      ""properties"": {
        ""subnets"": [
          {
            ""name"": ""mySubnet"",
          }
        ],
        ""enableDdosProtection"": true
      }
    },
    {
      ""apiVersion"": ""2015-06-15"",
      ""type"": ""Microsoft.Network/networkInterfaces"",
      ""name"": ""myNIC1"",
      ""location"": ""[parameters('location')]"",
      ""dependsOn"": [
        ""[concat('Microsoft.Network/virtualNetworks/', 'myVnet')]""
      ],
      ""properties"": {
        ""networkSecurityGroup"": {
          ""id"": ""[resourceId('Microsoft.Network/networkSecurityGroups', 'myNSG')]""
        },
        ""ipConfigurations"": [
          {
            ""name"": ""ipconfig1"",
            ""properties"": {
              ""privateIPAllocationMethod"": ""Dynamic"",
              ""subnet"": {
                ""id"": ""[resourceId('Microsoft.Network/virtualNetworks/subnets', 'myVnet', 'mySubnet')]""
              }
            }
          }
        ]
      }
    },
    {
      ""apiVersion"": ""2015-06-15"",
      ""type"": ""Microsoft.Network/networkInterfaces"",
      ""name"": ""myNIC2"",
      ""location"": ""[parameters('location')]"",
      ""dependsOn"": [
        ""[concat('Microsoft.Network/virtualNetworks/', 'myVnet')]""
      ],
      ""properties"": {
        ""ipConfigurations"": [
          {
            ""name"": ""ipconfig1"",
            ""properties"": {
              ""privateIPAllocationMethod"": ""Dynamic"",
              ""subnet"": {
                ""id"": ""[resourceId('Microsoft.Network/virtualNetworks/subnets', 'myVnet', 'mySubnet')]""
              }
            }
          }
        ]
      }
    },
    {
      ""apiVersion"": ""2016-04-30-preview"",
      ""type"": ""Microsoft.Compute/virtualMachines"",
      ""name"": ""myVM1"",
      ""location"": ""[parameters('location')]"",
      ""tags"": {
        ""Tag1"": ""VMtag"",
        ""Tag2"": ""AnotherTag""
      },
      ""dependsOn"": [
        ""[concat('Microsoft.Network/networkInterfaces/', 'myNIC')]""
      ],
      ""properties"": {
        ""osProfile"": {
          ""computerName"": ""vm1"",
        },
        ""networkProfile"": {
          ""networkInterfaces"": [
            {
              ""id"": ""[resourceId('Microsoft.Network/networkInterfaces', 'myNIC')]""
            }
          ]
        }
      }
    },
    {
      ""apiVersion"": ""2016-04-30-preview"",
      ""type"": ""Microsoft.Compute/virtualMachines"",
      ""name"": ""myVM2"",
      ""location"": ""[parameters('location')]"",
      ""tags"": {
        ""Tag1"": ""VMtag"",
        ""Tag2"": ""AnotherTag""
      },
      ""dependsOn"": [
        ""[concat('Microsoft.Network/networkInterfaces/', 'myNIC2')]""
      ],
      ""properties"": {
        ""osProfile"": {
          ""computerName"": ""vm2"",
          ""customData"": ""some data""
        },
        ""networkProfile"": {
          ""networkInterfaces"": [
            {
              ""id"": ""[resourceId('Microsoft.Network/networkInterfaces', 'myNIC2')]""
            }
          ]
        }
      }
    }
  ]
}";
        #endregion

        /// <summary>
        /// A mock implementation of an <see cref="Expression"/> for testing internal methods.
        /// </summary>
        private class MockExpression : Expression
        {
            public Action<IJsonPathResolver> EvaluationCallback { get; set; }

            public MockExpression(ExpressionCommonProperties commonProperties)
                : base(commonProperties)
            { }

            public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
            {
                return base.EvaluateInternal(jsonScope, scope =>
                {
                    EvaluationCallback(scope);
                    return new JsonRuleResult();
                });
            }
        }

        [DataTestMethod]
        [DataRow(null, "outputs",
            null,
            DisplayName = "No resource type, path not resolved")]
        [DataRow(null, "$schema",
            "$schema",
            DisplayName = "No resource type, path resolved")]
        [DataRow(null, "params.*",
            // No scopes expected evaluated
            DisplayName = "No resource type, wildcard path does not resolve")]
        [DataRow(null, "parameters.*",
            "parameters.location",
            DisplayName = "No resource type, wildcard path resolves single path")]
        [DataRow(null, "resources[*]",
            "resources[0]", "resources[1]", "resources[2]", "resources[3]", "resources[4]",
            DisplayName = "No resource type, wildcard path resolves multiple paths")]
        [DataRow("Microsoft.Storage/storageAccounts", null,
            // No scopes expected evaluated
            DisplayName = "Resource type matches none, no path")]
        [DataRow("Microsoft.Network/virtualNetworks", null,
            "resources[0]",
            DisplayName = "Resource type matches 1, no path")]
        [DataRow("Microsoft.Compute/virtualMachines", null,
            "resources[3]", "resources[4]",
            DisplayName = "Resource type matches multiple, no path")]
        [DataRow("Microsoft.Storage/storageAccounts", "name",
            // No scopes expected evaluated
            DisplayName = "Resource type matches none, path not resolved")]
        [DataRow("Microsoft.Network/virtualNetworks", "properties.addressSpace",
            null,
            DisplayName = "Resource type matches 1, path not resolved")]
        [DataRow("Microsoft.Network/virtualNetworks", "location",
            "resources[0].location",
            DisplayName = "Resource type matches 1, path resolved")]
        [DataRow("Microsoft.Network/virtualNetworks", "dependsOn[*]",
            // No scopes expected evaluated
            DisplayName = "Resource type matches 1, wildcard path does not resolve")]
        [DataRow("Microsoft.Network/virtualNetworks", "properties.subnets[*]",
            "resources[0].properties.subnets[0]",
            DisplayName = "Resource type matches 1, wildcard path resolves single path")]
        [DataRow("Microsoft.Network/virtualNetworks", "properties.*",
            "resources[0].properties.subnets[0]", "resources[0].properties.enableDdosProtection",
            DisplayName = "Resource type matches 1, wildcard path resolves multiple paths")]
        [DataRow("Microsoft.Network/networkInterfaces", "properties.dnsSettings",
            null, null,
            DisplayName = "Resource type matches multiple, path not resolved")]
        [DataRow("Microsoft.Network/networkInterfaces", "properties.networkSecurityGroup",
            "resources[1].properties.networkSecurityGroup", null,
            DisplayName = "Resource type matches multiple, path resolves in 1")]
        [DataRow("Microsoft.Network/networkInterfaces", "properties.ipConfigurations[0]",
            "resources[1].properties.ipConfigurations[0]", "resources[2].properties.ipConfigurations[0]",
            DisplayName = "Resource type matches multiple, path resolves in all")]
        [DataRow("Microsoft.Compute/virtualMachines", "properties.hardwareProfile.*",
            // No scopes expected evaluated
            DisplayName = "Resource type matches multiple, wildcard path does not resolve")]
        [DataRow("Microsoft.Compute/virtualMachines", "properties.*.customData",
            "resources[4].properties.osProfile.customData",
            DisplayName = "Resource type matches multiple, wildcard path resolves single path in 1")]
        [DataRow("Microsoft.Compute/virtualMachines", "properties.networkProfile.networkInterfaces[*]",
            "resources[3].properties.networkProfile.networkInterfaces[0]", "resources[4].properties.networkProfile.networkInterfaces[0]",
            DisplayName = "Resource type matches multiple, wildcard path resolves single path in all")]
        [DataRow("Microsoft.Compute/virtualMachines", "tags.*",
            "resources[3].tags.Tag1", "resources[3].tags.Tag2", "resources[4].tags.Tag1", "resources[4].tags.Tag2",
            DisplayName = "Resource type matches multiple, wildcard path resolves multiple paths")]
        public void EvaluateTemplate_ExpressionsWithVariousScopes_CorrectScopesAreEvaluated(string resourceType, string path, params string[] expectedPaths)
        {
            var scopesEvaluated = new List<IJsonPathResolver>();
            var expression = new MockExpression(new ExpressionCommonProperties { ResourceType = resourceType, Path = path })
            {
                // Track what scopes were called to evaluate with
                EvaluationCallback = scope => scopesEvaluated.Add(scope)
            };

            expression.Evaluate(new JsonPathResolver(JToken.Parse(mockTemplate), ""));

            Assert.AreEqual(expectedPaths.Length, scopesEvaluated.Count);

            // Verify all scopes evaluated (if any) were the expected scopes
            for (int i = 0; i < expectedPaths.Length; i++)
            {
                Assert.AreEqual(expectedPaths[i], scopesEvaluated[i].JToken?.Path, ignoreCase: true);
            }
        }
    }
}
