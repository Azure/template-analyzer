// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class DefaultStableAksVersionProviderTests
    {
        /// <summary>
        /// Test-specific provider that creates new instances for each test
        /// </summary>
        private class TestableDefaultStableAksVersionProvider : IStableAksVersionProvider
        {
            private readonly DefaultStableAksVersionProvider _inner;
            private readonly FieldInfo _cacheField;
            private readonly FieldInfo _lockField;

            public TestableDefaultStableAksVersionProvider()
            {
                // Use reflection to create a new instance bypassing the singleton
                var constructorInfo = typeof(DefaultStableAksVersionProvider)
                    .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
                
                _inner = (DefaultStableAksVersionProvider)constructorInfo.Invoke(null);
                
                _cacheField = typeof(DefaultStableAksVersionProvider)
                    .GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);
                _lockField = typeof(DefaultStableAksVersionProvider)
                    .GetField("_lock", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            public bool TryGetStableVersions(string normalizedLocation, out ISet<string> versions)
            {
                return _inner.TryGetStableVersions(normalizedLocation, out versions);
            }

            public void ResetCache()
            {
                var lockObject = _lockField.GetValue(_inner);
                lock (lockObject)
                {
                    _cacheField.SetValue(_inner, null);
                }
            }

            public bool IsCacheInitialized()
            {
                return _cacheField.GetValue(_inner) != null;
            }

            public DefaultStableAksVersionProvider GetInnerProvider() => _inner;
        }

        private TestableDefaultStableAksVersionProvider _testProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            // Create a fresh provider for each test
            _testProvider = new TestableDefaultStableAksVersionProvider();
            
            // Set it in the registry so HasStableAksVersionOperator will use it
            StableAksVersionProviderRegistry.SetProvider(_testProvider);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Reset to default provider after each test
            StableAksVersionProviderRegistry.ResetToDefault();
        }

        [TestMethod]
        public void Instance_WhenAccessed_ReturnsSameInstanceAlways()
        {
            // Test the actual singleton behavior
            var instance1 = DefaultStableAksVersionProvider.Instance;
            var instance2 = DefaultStableAksVersionProvider.Instance;
            
            Assert.IsNotNull(instance1);
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void TryGetStableVersions_WithNormalizedLocation_ReturnsConsistentResults()
        {
            // Verify cache is initially empty
            Assert.IsFalse(_testProvider.IsCacheInitialized(), "Cache should not be initialized at start");
            
            // First call - should initialize cache and fetch from API
            var result1 = _testProvider.TryGetStableVersions("eastus", out var versions1);
            
            // Verify cache is now populated
            Assert.IsTrue(_testProvider.IsCacheInitialized(), "Cache should be initialized after first call");
            
            // Second call - should use cached data
            var result2 = _testProvider.TryGetStableVersions("eastus", out var versions2);
            
            // Results should be consistent
            Assert.AreEqual(result1, result2);
            
            if (result1)
            {
                Assert.IsNotNull(versions1);
                Assert.IsNotNull(versions2);
                Assert.AreEqual(versions1.Count, versions2.Count);
                CollectionAssert.AreEquivalent(versions1.ToList(), versions2.ToList());
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void TryGetStableVersions_WithDifferentCasing_NormalizesCorrectly()
        {
            // This test will fetch fresh data since we have a new provider instance
            var result1 = _testProvider.TryGetStableVersions("eastus", out var versions1);
            var result2 = _testProvider.TryGetStableVersions("EASTUS", out var versions2);
            var result3 = _testProvider.TryGetStableVersions("EastUS", out var versions3);
            
            // All should return the same result due to normalization
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(result2, result3);
            
            if (result1)
            {
                Assert.IsTrue(versions1.Count > 0, "Should have versions for eastus");
                CollectionAssert.AreEquivalent(versions1.ToList(), versions2.ToList());
                CollectionAssert.AreEquivalent(versions2.ToList(), versions3.ToList());
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void TryGetStableVersions_WithSpacesInLocation_HandlesCorrectly()
        {
            // Test the actual behavior based on implementation
            var result1 = _testProvider.TryGetStableVersions("westeurope", out var versions1);
            var result2 = _testProvider.TryGetStableVersions("west europe", out var versions2);
            var result3 = _testProvider.TryGetStableVersions("West Europe", out var versions3);
            
            Assert.IsTrue(result1, "Should find westeurope (exact match with cache key)");
            Assert.IsFalse(result2, "Should NOT find 'west europe' (doesn't match normalized cache key)");
            Assert.IsFalse(result3, "Should NOT find 'West Europe' (doesn't match normalized cache key)");
            
            if (result1)
            {
                Assert.IsNotNull(versions1);
                Assert.IsTrue(versions1.Count > 0, "Should have versions for westeurope");
            }
            
            Assert.IsNull(versions2, "Should be null when not found");
            Assert.IsNull(versions3, "Should be null when not found");
        }

        [TestMethod]
        public void TryGetStableVersions_WithNullLocation_ReturnsFalse()
        {
            var result = _testProvider.TryGetStableVersions(null, out var versions);
            
            Assert.IsFalse(result);
            Assert.IsNull(versions);
            
            // Verify cache was not initialized for null input
            Assert.IsFalse(_testProvider.IsCacheInitialized(), "Cache should not be initialized for null input");
        }

        [TestMethod]
        public void TryGetStableVersions_WithEmptyLocation_ReturnsFalse()
        {
            var result1 = _testProvider.TryGetStableVersions("", out var versions1);
            Assert.IsFalse(_testProvider.IsCacheInitialized(), "Cache should not be initialized for empty input");
            var result2 = _testProvider.TryGetStableVersions("   ", out var versions2);
            Assert.IsTrue(_testProvider.IsCacheInitialized(), "Cache is initialized now");
            
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
            Assert.IsNull(versions1);
            Assert.IsNull(versions2);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void TryGetStableVersions_WithUnknownLocation_ReturnsFalse()
        {
            // This will initialize the cache with real data
            var knownResult = _testProvider.TryGetStableVersions("eastus", out _);
            
            // Now test with unknown locations
            var result1 = _testProvider.TryGetStableVersions("nonexistentregion", out var versions1);
            var result2 = _testProvider.TryGetStableVersions("marscentral", out var versions2);
            
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
            Assert.IsNull(versions1);
            Assert.IsNull(versions2);
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void EnsureInitialized_CalledMultipleTimes_OnlyFetchesOnce()
        {
            Assert.IsFalse(_testProvider.IsCacheInitialized(), "Cache should not be initialized");
            
            // First call should fetch data
            var startTime = DateTime.UtcNow;
            _testProvider.TryGetStableVersions("eastus", out _);
            var firstCallTime = DateTime.UtcNow - startTime;
            
            Assert.IsTrue(_testProvider.IsCacheInitialized(), "Cache should be initialized after first call");
            
            // Second call should be much faster (no fetch)
            startTime = DateTime.UtcNow;
            _testProvider.TryGetStableVersions("westus", out _);
            var secondCallTime = DateTime.UtcNow - startTime;
            
            // Second call should be significantly faster
            Assert.IsTrue(secondCallTime.TotalMilliseconds < firstCallTime.TotalMilliseconds / 10 || 
                         secondCallTime.TotalMilliseconds < 50,
                $"Second call ({secondCallTime.TotalMilliseconds}ms) should be much faster than first ({firstCallTime.TotalMilliseconds}ms)");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void TryGetStableVersions_WhenDataExists_ReturnsValidVersionFormats()
        {
            var result = _testProvider.TryGetStableVersions("eastus", out var versions);
            
            if (result)
            {
                Assert.IsNotNull(versions);
                Assert.IsTrue(versions.Count > 0, "Should have at least one version");
                
                foreach (var version in versions)
                {
                    // Verify semantic versioning format
                    Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+"), 
                        $"Version '{version}' should follow semantic versioning");
                    
                    // Verify no HTML or markers
                    Assert.IsFalse(version.Contains("<"), $"Version '{version}' should not contain HTML");
                    Assert.IsFalse(version.Contains("(LTS)"), $"Version '{version}' should not contain LTS marker");
                }
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void TryGetStableVersions_ThreadSafety_HandlesMultipleSimultaneousRequests()
        {
            var results = new bool[10];
            var versionCounts = new int[10];
            var exceptions = new List<Exception>();
            
            // Test thread safety with parallel requests
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                try
                {
                    results[i] = _testProvider.TryGetStableVersions("eastus", out var versions);
                    versionCounts[i] = versions?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            
            // Should not have any exceptions
            Assert.AreEqual(0, exceptions.Count, 
                $"Thread safety test failed with exceptions: {string.Join(", ", exceptions.Select(e => e.Message))}");
            
            // All calls should return the same result
            var firstResult = results[0];
            Assert.IsTrue(results.All(r => r == firstResult), "All parallel calls should succeed/fail consistently");
            
            if (firstResult)
            {
                var firstCount = versionCounts[0];
                Assert.IsTrue(versionCounts.All(c => c == firstCount), "All parallel calls should return same version count");
            }
        }
    }
}