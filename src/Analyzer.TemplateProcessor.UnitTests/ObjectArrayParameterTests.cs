// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Azure.Deployments.Templates.Engines;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.TemplateProcessor.UnitTests
{
    [TestClass]
    public class ObjectArrayParameterTests
    {
        private static JObject CreateTemplate() => JObject.Parse("""
            {
              "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
              "contentVersion": "1.0.0.0",
              "parameters": { "rules": { "type": "array" } },
              "resources": [
                { "type": "Microsoft.Sql/servers", "apiVersion": "2014-04-01", "name": "sql" },
                {
                  "type": "Microsoft.Sql/servers/firewallRules",
                  "apiVersion": "2021-11-01",
                  "name": "[concat('sql/', parameters('rules')[copyIndex()].name)]",
                  "copy": { "name": "firewalls", "count": "[length(parameters('rules'))]" },
                  "properties": { "startIpAddress": "192.0.2.1" }
                }
              ]
            }
            """);

        [TestMethod]
        public void ProcessTemplate_ObjectArrayWithoutDefault_PreservesCopiesAndSourceMappings()
        {
            var template = CreateTemplate();
            var processor = new ArmTemplateProcessor(template.ToString());
            var processed = processor.ProcessTemplate();
            var resources = (JArray)processed["resources"];

            Assert.AreEqual(3, resources.Count);
            Assert.AreEqual("2014-04-01", (string)resources[0]["apiVersion"]);
            for (int i = 1; i <= 2; i++)
            {
                Assert.AreEqual((string)template["resources"][1]["name"], (string)resources[i]["name"]);
                Assert.AreEqual("Microsoft.Sql/servers/firewallRules", (string)resources[i]["type"]);
                Assert.AreEqual("192.0.2.1", (string)resources[i]["properties"]["startIpAddress"]);
                Assert.AreEqual("resources[1]", processor.ResourceMappings[$"resources[{i}]"]);
            }
            Assert.AreEqual("resources[0]", processor.ResourceMappings["resources[0]"]);
            Assert.IsNull(processed["parameters"]["rules"]["defaultValue"]);
        }

        [DataTestMethod]
        [DataRow(false, 1, DisplayName = "Single object-array default")]
        [DataRow(true, 1, DisplayName = "Single supplied object")]
        [DataRow(false, 2, DisplayName = "Multiple object-array defaults")]
        [DataRow(true, 2, DisplayName = "Multiple supplied objects")]
        public void ProcessTemplate_ObjectArrayValues_ResolvesNames(bool suppliedParameters, int count)
        {
            var template = CreateTemplate();
            var values = JArray.Parse(@"[{""name"":""office""},{""name"":""home""}]");
            if (count == 1)
            {
                values.Last.Remove();
            }
            string parameters = null;
            if (suppliedParameters)
            {
                parameters = new JObject { ["parameters"] = new JObject { ["rules"] = new JObject { ["value"] = values } } }.ToString();
            }
            else
            {
                template["parameters"]["rules"]["defaultValue"] = values;
            }

            var processor = new ArmTemplateProcessor(template.ToString());
            var resources = (JArray)processor.ProcessTemplate(parameters)["resources"];

            CollectionAssert.AreEqual(new[] { "sql", "sql/office", "sql/home" }.Take(count + 1).ToArray(),
                resources.Select(resource => (string)resource["name"]).ToArray());
            for (int i = 1; i <= count; i++)
            {
                Assert.AreEqual("resources[1]", processor.ResourceMappings[$"resources[{i}]"]);
            }
        }

        [TestMethod]
        public void ProcessTemplate_StringArrayWithoutDefault_ResolvesNames()
        {
            var template = CreateTemplate();
            template["resources"][1]["name"] = "[concat('sql/', parameters('rules')[copyIndex()])]";

            var resources = (JArray)new ArmTemplateProcessor(template.ToString()).ProcessTemplate()["resources"];

            CollectionAssert.AreEqual(new[] { "sql", "sql/item1", "sql/item2" },
                resources.Select(resource => (string)resource["name"]).ToArray());
        }

        [TestMethod]
        public void ProcessTemplate_UnusedArrayWithoutDefault_PreservesLiteralResource()
        {
            var template = CreateTemplate();
            template["resources"][1].Remove();
            var processor = new ArmTemplateProcessor(template.ToString());
            var resources = (JArray)processor.ProcessTemplate()["resources"];

            Assert.AreEqual(1, resources.Count);
            Assert.AreEqual("sql", (string)resources[0]["name"]);
            Assert.AreEqual("2014-04-01", (string)resources[0]["apiVersion"]);
            Assert.AreEqual("resources[0]", processor.ResourceMappings["resources[0]"]);
        }

        [TestMethod]
        public void ProcessTemplate_UnresolvedCopiesWithDependencies_PreservesEveryInstanceOnce()
        {
            var template = CreateTemplate();
            template["resources"][1]["dependsOn"] = new JArray("sql", "sql");
            template["resources"][1]["properties"]["index"] = "[copyIndex()]";
            var processor = new ArmTemplateProcessor(template.ToString());
            var resources = (JArray)processor.ProcessTemplate()["resources"];
            var children = (JArray)resources[0]["resources"];

            Assert.AreEqual(3, resources.Count);
            Assert.AreEqual(2, children.Count);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(i, (int)children[i]["properties"]["index"]);
                Assert.AreEqual(i, (int)resources[i + 1]["properties"]["index"]);
                Assert.AreEqual("resources[1]", processor.ResourceMappings[$"resources[0].resources[{i}]"]);
                Assert.AreEqual("resources[1]", processor.ResourceMappings[$"resources[{i + 1}]"]);
            }
        }

        [TestMethod]
        public void ProcessTemplate_UnresolvedCopiedParents_PreservesNestedResourceMappings()
        {
            var template = CreateTemplate();
            var parent = template["resources"][1];
            parent["name"] = "[parameters('rules')[copyIndex()].name]";
            parent["type"] = "Microsoft.Sql/servers";
            parent["resources"] = JArray.Parse(@"[{
                ""name"": ""office"",
                ""type"": ""firewallRules"",
                ""apiVersion"": ""2021-11-01"",
                ""properties"": { ""value"": ""[add(1, 1)]"" }
            }]");
            var processor = new ArmTemplateProcessor(template.ToString());
            var resources = (JArray)processor.ProcessTemplate()["resources"];

            Assert.AreEqual(3, resources.Count);
            for (int i = 1; i <= 2; i++)
            {
                var child = resources[i]["resources"].Single();
                Assert.AreEqual("office", (string)child["name"]);
                Assert.AreEqual("Microsoft.Sql/servers/firewallRules", (string)child["type"]);
                Assert.AreEqual(2, (int)child["properties"]["value"]);
                Assert.AreEqual("resources[1].resources[0]", processor.ResourceMappings[$"resources[{i}].resources[0]"]);
            }
        }

        [TestMethod]
        public void ProcessResourcesAndOutputs_UnresolvedParent_DoesNotGuessNameBasedDependency()
        {
            var template = CreateTemplate();
            var unresolvedName = (string)template["resources"][1]["name"];
            ((JObject)template["resources"][1]).Remove("copy");
            template["resources"][0]["dependsOn"] = new JArray(unresolvedName);
            var processor = new ArmTemplateProcessor(template.ToString());
            var processed = processor.ProcessResourcesAndOutputs(TemplateEngine.ParseTemplate(template.ToString()));

            Assert.AreEqual(2, processed.Resources.Length);
            Assert.IsNull(processed.Resources[1].Resources);
            Assert.AreEqual("Microsoft.Sql/servers/firewallRules", processed.Resources[1].Type.Value);
        }

        [DataTestMethod]
        [DataRow("sql", DisplayName = "Identical literal declarations")]
        [DataRow("SQL", DisplayName = "Case-insensitive literal declarations")]
        public void ProcessResourcesAndOutputs_DuplicateDeclarations_ThrowsExplicitError(string duplicateName)
        {
            var template = CreateTemplate();
            var duplicate = template["resources"][0].DeepClone();
            duplicate["name"] = duplicateName;
            template["resources"] = new JArray(template["resources"][0], duplicate);
            var processor = new ArmTemplateProcessor(template.ToString());

            var exception = Assert.ThrowsException<ArgumentException>(() =>
                processor.ProcessResourcesAndOutputs(TemplateEngine.ParseTemplate(template.ToString())));

            StringAssert.Contains(exception.Message.ToLowerInvariant(), "sql microsoft.sql/servers");
        }

        [DataTestMethod]
        [DataRow(false, DisplayName = "Resolved parent")]
        [DataRow(true, DisplayName = "Unresolved parent")]
        public void ProcessResourcesAndOutputs_DuplicateNestedDeclarations_ThrowsExplicitError(bool unresolvedParent)
        {
            var template = CreateTemplate();
            var child = JObject.Parse(@"{""name"":""office"",""type"":""firewallRules"",""apiVersion"":""2021-11-01""}");
            template["resources"][1].Remove();
            if (unresolvedParent)
            {
                template["resources"][0]["name"] = "[parameters('rules')[0].name]";
            }
            template["resources"][0]["resources"] = new JArray(child, child.DeepClone());
            var processor = new ArmTemplateProcessor(template.ToString());

            var exception = Assert.ThrowsException<ArgumentException>(() =>
                processor.ProcessResourcesAndOutputs(TemplateEngine.ParseTemplate(template.ToString())));

            StringAssert.Contains(exception.Message, "/office Microsoft.Sql/servers/firewallRules");
        }

        [DataTestMethod]
        [DataRow(false, DisplayName = "Resolved object-array duplicates")]
        [DataRow(true, DisplayName = "Constant name in a copy loop")]
        public void ProcessTemplate_DuplicateResolvedCopies_ThrowsExplicitError(bool constantName)
        {
            var template = CreateTemplate();
            template["parameters"]["rules"]["defaultValue"] = JArray.Parse(@"[{""name"":""office""},{""name"":""office""}]");
            if (constantName)
            {
                template["resources"][1]["name"] = "sql/office";
            }
            var processor = new ArmTemplateProcessor(template.ToString());

            var exception = Assert.ThrowsException<ArgumentException>(() => processor.ProcessTemplate());

            StringAssert.Contains(exception.Message, "sql/office Microsoft.Sql/servers/firewallRules");
        }

        [TestMethod]
        public void ProcessResourcesAndOutputs_DuplicateNestedAndTopLevelIdentity_ThrowsExplicitError()
        {
            var template = CreateTemplate();
            template["resources"][0]["resources"] = JArray.Parse(@"[{
                ""name"": ""office"", ""type"": ""firewallRules"", ""apiVersion"": ""2021-11-01""
            }]");
            template["resources"][1]["name"] = "sql/office";
            ((JObject)template["resources"][1]).Remove("copy");
            var processor = new ArmTemplateProcessor(template.ToString());

            var exception = Assert.ThrowsException<ArgumentException>(() =>
                processor.ProcessResourcesAndOutputs(TemplateEngine.ParseTemplate(template.ToString())));

            StringAssert.Contains(exception.Message, "sql/office Microsoft.Sql/servers/firewallRules");
        }
    }
}
