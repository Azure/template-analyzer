// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// Registry for the stable AKS version provider.
    /// </summary>
    internal static class StableAksVersionProviderRegistry
    {
        private static IStableAksVersionProvider _provider = DefaultStableAksVersionProvider.Instance;

        /// <summary>
        /// Gets the current provider.
        /// </summary>
        public static IStableAksVersionProvider Provider => _provider;

        /// <summary>
        /// Sets a custom provider (mainly for testing).
        /// </summary>
        /// <param name="provider">The provider to use.</param>
        public static void SetProvider(IStableAksVersionProvider provider)
        {
            _provider = provider ?? DefaultStableAksVersionProvider.Instance;
        }

        /// <summary>
        /// Resets to the default provider.
        /// </summary>
        public static void ResetToDefault()
        {
            _provider = DefaultStableAksVersionProvider.Instance;
        }
    }
}