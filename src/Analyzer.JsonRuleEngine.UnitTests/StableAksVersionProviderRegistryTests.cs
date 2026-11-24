// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class StableAksVersionProviderRegistryTests
    {
        private InMemoryStableAksVersionProvider _testProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create a test provider with known data
            var testData = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["testregion"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1.0.0", "1.1.0" }
            };
            _testProvider = new InMemoryStableAksVersionProvider(testData);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Reset to default provider after each test to avoid test interference
            StableAksVersionProviderRegistry.ResetToDefault();
        }

        [TestMethod]
        public void Provider_ByDefault_ReturnsDefaultStableAksVersionProvider()
        {
            // Reset to ensure we're testing the default state
            StableAksVersionProviderRegistry.ResetToDefault();
            
            var provider = StableAksVersionProviderRegistry.Provider;
            
            Assert.IsNotNull(provider);
            Assert.IsInstanceOfType(provider, typeof(DefaultStableAksVersionProvider));
            Assert.AreSame(DefaultStableAksVersionProvider.Instance, provider);
        }

        [TestMethod]
        public void Provider_AfterSettingCustomProvider_ReturnsCustomProvider()
        {
            StableAksVersionProviderRegistry.SetProvider(_testProvider);
            
            var provider = StableAksVersionProviderRegistry.Provider;
            
            Assert.IsNotNull(provider);
            Assert.AreSame(_testProvider, provider);
            Assert.IsInstanceOfType(provider, typeof(InMemoryStableAksVersionProvider));
        }

        [TestMethod]
        public void SetProvider_WithValidProvider_SetsProviderCorrectly()
        {
            // Verify initial state
            Assert.IsInstanceOfType(StableAksVersionProviderRegistry.Provider, typeof(DefaultStableAksVersionProvider));
            
            // Set custom provider
            StableAksVersionProviderRegistry.SetProvider(_testProvider);
            
            // Verify the provider was set
            Assert.AreSame(_testProvider, StableAksVersionProviderRegistry.Provider);
        }

        [TestMethod]
        public void SetProvider_WithNull_SetsToDefaultProvider()
        {
            // First set to a custom provider
            StableAksVersionProviderRegistry.SetProvider(_testProvider);
            Assert.AreSame(_testProvider, StableAksVersionProviderRegistry.Provider);
            
            // Then set to null
            StableAksVersionProviderRegistry.SetProvider(null);
            
            // Should revert to default provider
            Assert.IsInstanceOfType(StableAksVersionProviderRegistry.Provider, typeof(DefaultStableAksVersionProvider));
            Assert.AreSame(DefaultStableAksVersionProvider.Instance, StableAksVersionProviderRegistry.Provider);
        }

        [TestMethod]
        public void ResetToDefault_AfterSettingCustomProvider_RestoresDefaultProvider()
        {
            // Set custom provider
            StableAksVersionProviderRegistry.SetProvider(_testProvider);
            Assert.AreSame(_testProvider, StableAksVersionProviderRegistry.Provider);
            
            // Reset to default
            StableAksVersionProviderRegistry.ResetToDefault();
            
            // Should be back to default provider
            Assert.IsInstanceOfType(StableAksVersionProviderRegistry.Provider, typeof(DefaultStableAksVersionProvider));
            Assert.AreSame(DefaultStableAksVersionProvider.Instance, StableAksVersionProviderRegistry.Provider);
        }

        [TestMethod]
        public void ResetToDefault_WhenAlreadyDefault_RemainsDefault()
        {
            // Ensure we start with default
            StableAksVersionProviderRegistry.ResetToDefault();
            var providerBefore = StableAksVersionProviderRegistry.Provider;
            
            // Reset again
            StableAksVersionProviderRegistry.ResetToDefault();
            var providerAfter = StableAksVersionProviderRegistry.Provider;
            
            // Should still be the same default provider
            Assert.AreSame(providerBefore, providerAfter);
            Assert.IsInstanceOfType(providerAfter, typeof(DefaultStableAksVersionProvider));
            Assert.AreSame(DefaultStableAksVersionProvider.Instance, providerAfter);
        }

        [TestMethod]
        public void Provider_MultipleAccesses_ReturnsSameInstance()
        {
            StableAksVersionProviderRegistry.SetProvider(_testProvider);
            
            var provider1 = StableAksVersionProviderRegistry.Provider;
            var provider2 = StableAksVersionProviderRegistry.Provider;
            var provider3 = StableAksVersionProviderRegistry.Provider;
            
            Assert.AreSame(provider1, provider2);
            Assert.AreSame(provider2, provider3);
            Assert.AreSame(_testProvider, provider1);
        }

        [TestMethod]
        public void ProviderChanges_AreReflectedImmediately()
        {
            // Start with default
            var defaultProvider = StableAksVersionProviderRegistry.Provider;
            Assert.IsInstanceOfType(defaultProvider, typeof(DefaultStableAksVersionProvider));
            
            // Change to custom provider
            StableAksVersionProviderRegistry.SetProvider(_testProvider);
            var customProvider = StableAksVersionProviderRegistry.Provider;
            Assert.AreSame(_testProvider, customProvider);
            Assert.AreNotSame(defaultProvider, customProvider);
            
            // Change back to default
            StableAksVersionProviderRegistry.ResetToDefault();
            var backToDefaultProvider = StableAksVersionProviderRegistry.Provider;
            Assert.IsInstanceOfType(backToDefaultProvider, typeof(DefaultStableAksVersionProvider));
            Assert.AreNotSame(customProvider, backToDefaultProvider);
        }

        [TestMethod]
        public void SetProvider_WithDifferentCustomProviders_UpdatesCorrectly()
        {
            var firstTestData = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["region1"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "1.0.0" }
            };
            var firstProvider = new InMemoryStableAksVersionProvider(firstTestData);
            
            var secondTestData = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["region2"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "2.0.0" }
            };
            var secondProvider = new InMemoryStableAksVersionProvider(secondTestData);
            
            // Set first provider
            StableAksVersionProviderRegistry.SetProvider(firstProvider);
            Assert.AreSame(firstProvider, StableAksVersionProviderRegistry.Provider);
            
            // Set second provider
            StableAksVersionProviderRegistry.SetProvider(secondProvider);
            Assert.AreSame(secondProvider, StableAksVersionProviderRegistry.Provider);
            Assert.AreNotSame(firstProvider, StableAksVersionProviderRegistry.Provider);
        }
    }
}