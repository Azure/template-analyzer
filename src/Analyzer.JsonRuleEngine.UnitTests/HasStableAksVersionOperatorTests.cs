// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class HasStableAksVersionOperatorTests
    {
        private InMemoryStableAksVersionProvider _mockProvider;

        private static readonly Dictionary<string, HashSet<string>> MockData = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["eastus"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1.30.12", "1.29.15", "1.28.101" },
            ["westus"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1.30.12", "1.29.14", "1.28.100" },
            ["qatarcentral"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1.33.2", "1.32.6", "1.31.10" },
            ["westeurope"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1.30.12", "1.29.15" }
        };

        [TestInitialize]
        public void TestInitialize()
        {
            _mockProvider = new InMemoryStableAksVersionProvider(MockData);
            StableAksVersionProviderRegistry.SetProvider(_mockProvider);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            StableAksVersionProviderRegistry.ResetToDefault();
        }

        [DataTestMethod]
        [DataRow("eastus", "1.30.12", DisplayName = "Stable version in East US")]
        [DataRow("westus", "1.29.14", DisplayName = "Stable version in West US")]
        [DataRow("Qatar Central", "1.33.2", DisplayName = "Stable version in Qatar Central with spaces")]
        [DataRow("WESTEUROPE", "1.30.12", DisplayName = "Stable version with uppercase location")]
        [DataRow("WestEurope", "1.29.15", DisplayName = "Stable version with mixed case location")]
        public void EvaluateExpression_StableVersion_HasStableAksVersionIsTrue(string location, string version)
        {
            var aksResource = TestUtilities.CreateAksResource(location, version);

            // Test using default constructor (uses registry)
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false);
            Assert.IsTrue(hasStableOperator.EvaluateExpression(aksResource));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false);
            Assert.IsFalse(doesNotHaveStableOperator.EvaluateExpression(aksResource));

            // Test using dependency injection constructor
            var hasStableOperatorDI = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.IsTrue(hasStableOperatorDI.EvaluateExpression(aksResource));

            var doesNotHaveStableOperatorDI = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, _mockProvider);
            Assert.IsFalse(doesNotHaveStableOperatorDI.EvaluateExpression(aksResource));
        }

        [DataTestMethod]
        [DataRow("eastus", "1.31.0", DisplayName = "Unstable version in East US")]
        [DataRow("westus", "1.27.100", DisplayName = "Unstable version in West US")]
        [DataRow("qatarcentral", "1.30.12", DisplayName = "Version not available in Qatar Central")]
        [DataRow("West Europe", "1.28.100", DisplayName = "Version not available in West Europe")]
        public void EvaluateExpression_UnstableVersion_HasStableAksVersionIsFalse(string location, string version)
        {
            var aksResource = TestUtilities.CreateAksResource(location, version);

            // Test using default constructor (uses registry)
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(aksResource));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false);
            Assert.IsTrue(doesNotHaveStableOperator.EvaluateExpression(aksResource));

            // Test using dependency injection constructor
            var hasStableOperatorDI = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.IsFalse(hasStableOperatorDI.EvaluateExpression(aksResource));

            var doesNotHaveStableOperatorDI = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, _mockProvider);
            Assert.IsTrue(doesNotHaveStableOperatorDI.EvaluateExpression(aksResource));
        }

        [DataTestMethod]
        [DataRow("unknownregion", "1.30.12", DisplayName = "Unknown region")]
        [DataRow("mars-central", "1.29.15", DisplayName = "Non-existent region")]
        public void EvaluateExpression_UnknownLocation_TreatedAsUnstable(string location, string version)
        {
            var aksResource = TestUtilities.CreateAksResource(location, version);

            // Test using default constructor (uses registry)
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(aksResource));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false);
            Assert.IsTrue(doesNotHaveStableOperator.EvaluateExpression(aksResource));

            // Test using dependency injection constructor
            var hasStableOperatorDI = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.IsFalse(hasStableOperatorDI.EvaluateExpression(aksResource));

            var doesNotHaveStableOperatorDI = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, _mockProvider);
            Assert.IsTrue(doesNotHaveStableOperatorDI.EvaluateExpression(aksResource));
        }

        [TestMethod]
        public void EvaluateExpression_MissingLocation_TreatedAsUnstable()
        {
            var resource = new
            {
                type = "Microsoft.ContainerService/managedClusters",
                properties = new
                {
                    kubernetesVersion = "1.30.2",
                }
            };
            
            var jObjectResource = JObject.FromObject(resource);

            // Test using dependency injection constructor
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(jObjectResource));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, _mockProvider);
            Assert.IsTrue(doesNotHaveStableOperator.EvaluateExpression(jObjectResource));
        }

        [TestMethod]
        public void EvaluateExpression_MissingKubernetesVersion_TreatedAsUnstable()
        {
            var resource = new
            {
                type = "Microsoft.ContainerService/managedClusters",
                location = "eastus",
                properties = new
                {
                    linuxProfile = new
                    {
                        adminUsername = "azureuser",
                        ssh = new
                        {
                            publicKeys = new[]
                            {
                                new { keyData = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQ..." }
                            }
                        }
                    }
                }
            };
            
            var jObjectResource = JObject.FromObject(resource);

            // Test using dependency injection constructor
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(jObjectResource));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, _mockProvider);
            Assert.IsTrue(doesNotHaveStableOperator.EvaluateExpression(jObjectResource));
        }

        [TestMethod]
        public void EvaluateExpression_MissingProperties_TreatedAsUnstable()
        {
            var resource = new
            {
                type = "Microsoft.ContainerService/managedClusters",
                apiVersion = "2025-06-02-preview",
                name = "test-aks-cluster",
                location = "eastus"
            };
            
            var jObjectResource = JObject.FromObject(resource);

            // Test using dependency injection constructor
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(jObjectResource));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, _mockProvider);
            Assert.IsTrue(doesNotHaveStableOperator.EvaluateExpression(jObjectResource));
        }

        [TestMethod]
        public void EvaluateExpression_NullToken_TreatedAsUnstable()
        {
            // Test using dependency injection constructor
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(null));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, _mockProvider);
            Assert.IsTrue(doesNotHaveStableOperator.EvaluateExpression(null));
        }

        [DataTestMethod]
        [DataRow("", "1.30.12", DisplayName = "Empty location string")]
        [DataRow("eastus", "", DisplayName = "Empty version string")]
        [DataRow("", "", DisplayName = "Both empty strings")]
        public void EvaluateExpression_EmptyStrings_TreatedAsUnstable(string location, string version)
        {
            var aksResource = TestUtilities.CreateAksResource(location, version);

            // Test using dependency injection constructor
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(aksResource));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, _mockProvider);
            Assert.IsTrue(doesNotHaveStableOperator.EvaluateExpression(aksResource));
        }

        [TestMethod]
        public void EvaluateExpression_WithIsNegative_InvertsResult()
        {
            var aksResource = TestUtilities.CreateAksResource("eastus", "1.30.12");

            // Stable version with isNegative: true should invert the result
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: true, _mockProvider);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(aksResource));

            // Unstable version
            aksResource = TestUtilities.CreateAksResource("eastus", "1.31.0");
            hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: true, _mockProvider);
            Assert.IsTrue(hasStableOperator.EvaluateExpression(aksResource));
        }

        [TestMethod]
        public void Name_WhenAccessed_ReturnsHasStableAksVersion()
        {
            // Test both constructors return the same name
            Assert.AreEqual("HasStableAksVersion", new HasStableAksVersionOperator(true, false).Name);
            Assert.AreEqual("HasStableAksVersion", new HasStableAksVersionOperator(true, true).Name);
            Assert.AreEqual("HasStableAksVersion", new HasStableAksVersionOperator(false, false).Name);
            Assert.AreEqual("HasStableAksVersion", new HasStableAksVersionOperator(false, true).Name);

            Assert.AreEqual("HasStableAksVersion", new HasStableAksVersionOperator(true, false, _mockProvider).Name);
            Assert.AreEqual("HasStableAksVersion", new HasStableAksVersionOperator(true, true, _mockProvider).Name);
            Assert.AreEqual("HasStableAksVersion", new HasStableAksVersionOperator(false, false, _mockProvider).Name);
            Assert.AreEqual("HasStableAksVersion", new HasStableAksVersionOperator(false, true, _mockProvider).Name);
        }

        [TestMethod]
        public void Constructor_WhenCalledWithValidParameters_SetsPropertiesCorrectly()
        {
            // Test default constructor
            var operatorTrue = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false);
            Assert.AreEqual(true, operatorTrue.SpecifiedValue.Value<bool>());
            Assert.AreEqual(false, operatorTrue.IsNegative);

            var operatorFalse = new HasStableAksVersionOperator(specifiedValue: false, isNegative: true);
            Assert.AreEqual(false, operatorFalse.SpecifiedValue.Value<bool>());
            Assert.AreEqual(true, operatorFalse.IsNegative);

            // Test dependency injection constructor
            var operatorTrueDI = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, _mockProvider);
            Assert.AreEqual(true, operatorTrueDI.SpecifiedValue.Value<bool>());
            Assert.AreEqual(false, operatorTrueDI.IsNegative);

            var operatorFalseDI = new HasStableAksVersionOperator(specifiedValue: false, isNegative: true, _mockProvider);
            Assert.AreEqual(false, operatorFalseDI.SpecifiedValue.Value<bool>());
            Assert.AreEqual(true, operatorFalseDI.IsNegative);
        }

        [TestMethod]
        public void Constructor_ThrowsArgumentNullException_WhenProviderIsNull()
        {
            Assert.ThrowsException<ArgumentNullException>(() => 
                new HasStableAksVersionOperator(true, false, null));
        }

        [TestMethod]
        public void EvaluateExpression_WithEmptyProvider_TreatsAllAsUnstable()
        {
            var emptyProvider = new InMemoryStableAksVersionProvider(new Dictionary<string, HashSet<string>>());
            var aksResource = TestUtilities.CreateAksResource("eastus", "1.30.12");
            
            // With empty provider, all versions are treated as unstable
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false, emptyProvider);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(aksResource));

            var doesNotHaveStableOperator = new HasStableAksVersionOperator(specifiedValue: false, isNegative: false, emptyProvider);
            Assert.IsTrue(doesNotHaveStableOperator.EvaluateExpression(aksResource));
        }

        [TestMethod]
        public void Registry_WhenSetAndResetProvider_ChangesProviderCorrectly()
        {
            var customProvider = new InMemoryStableAksVersionProvider(
                new Dictionary<string, HashSet<string>>
                {
                    ["testregion"] = new HashSet<string> { "1.0.0" }
                });

            // Set custom provider
            StableAksVersionProviderRegistry.SetProvider(customProvider);
            
            var aksResource = TestUtilities.CreateAksResource("testregion", "1.0.0");
            var hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false);
            Assert.IsTrue(hasStableOperator.EvaluateExpression(aksResource));

            // Reset to default
            StableAksVersionProviderRegistry.ResetToDefault();
            
            // After reset, should not find the test region (since default provider won't have it)
            hasStableOperator = new HasStableAksVersionOperator(specifiedValue: true, isNegative: false);
            Assert.IsFalse(hasStableOperator.EvaluateExpression(aksResource));
        }
    }

    /// <summary>
    /// In-memory implementation of IStableAksVersionProvider for testing.
    /// </summary>
    internal class InMemoryStableAksVersionProvider : IStableAksVersionProvider
    {
        private readonly Dictionary<string, HashSet<string>> _data;

        public InMemoryStableAksVersionProvider(Dictionary<string, HashSet<string>> data)
        {
            _data = data ?? new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetStableVersions(string normalizedLocation, out ISet<string> versions)
        {
            if (_data.TryGetValue(normalizedLocation, out var set))
            {
                versions = set;
                return true;
            }
            
            versions = null;
            return false;
        }
    }
}