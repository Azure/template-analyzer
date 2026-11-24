// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// An operator that evaluates whether an Aks cluster is using a stable Kubernetes version for its region.
    /// </summary>
    internal class HasStableAksVersionOperator : LeafExpressionOperator
    {
        private readonly IStableAksVersionProvider _provider;

        /// <summary>
        /// Gets the name of this operator.
        /// </summary>
        public override string Name => "HasStableAksVersion";

        /// <summary>
        /// Creates a HasStableAksVersionOperator using the default provider from the registry.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule.</param>
        /// <param name="isNegative">Whether the result should be negated.</param>
        public HasStableAksVersionOperator(bool specifiedValue, bool isNegative)
            : this(specifiedValue, isNegative, StableAksVersionProviderRegistry.Provider)
        {
        }

        /// <summary>
        /// Creates a HasStableAksVersionOperator with a specific provider.
        /// </summary>
        /// <param name="specifiedValue">The value specified in the JSON rule.</param>
        /// <param name="isNegative">Whether the result should be negated.</param>
        /// <param name="provider">The provider to use for getting stable versions.</param>
        internal HasStableAksVersionOperator(bool specifiedValue, bool isNegative, IStableAksVersionProvider provider)
        {
            this.SpecifiedValue = JToken.FromObject(specifiedValue);
            this.IsNegative = isNegative;
            this._provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Evaluates whether the Aks cluster is using a stable version.
        /// </summary>
        /// <param name="tokenToEvaluate">The JToken representing the Aks cluster resource.</param>
        /// <returns>True if the evaluation passes, false otherwise.</returns>
        public override bool EvaluateExpression(JToken tokenToEvaluate)
        {
            bool specifiedBoolValue = this.SpecifiedValue.Value<bool>();

            var resource = tokenToEvaluate?.Parent?.Parent;
            var location = resource?["location"]?.Value<string>();
            var kubernetesVersion = tokenToEvaluate?["kubernetesVersion"]?.Value<string>();

            if (string.IsNullOrEmpty(location) || string.IsNullOrEmpty(kubernetesVersion))
            {
                // Missing data is treated as not having stable version
                return !specifiedBoolValue ^ this.IsNegative;
            }

            var normalizedLocation = NormalizeRegionName(location);

            if (!_provider.TryGetStableVersions(normalizedLocation, out var stableVersions))
            {
                // Unknown location is treated as not having stable version
                return !specifiedBoolValue ^ this.IsNegative;
            }

            var isStableVersion = stableVersions.Contains(kubernetesVersion);
            return (isStableVersion == specifiedBoolValue) ^ this.IsNegative;
        }

    
        /// <summary>
        /// Normalizes a region name by converting to lowercase and removing spaces.
        /// </summary>
        /// <param name="regionName">The region name to normalize.</param>
        /// <returns>The normalized region name.</returns>
        private static string NormalizeRegionName(string regionName)
        {
            return regionName.ToLowerInvariant().Replace(" ", "");
        }
    }
}