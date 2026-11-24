// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// Provides stable AKS versions by location.
    /// </summary>
    internal interface IStableAksVersionProvider
    {
        /// <summary>
        /// Attempts to get stable AKS versions for a normalized location.
        /// </summary>
        /// <param name="normalizedLocation">The normalized location name (lowercase, no spaces).</param>
        /// <param name="versions">The set of stable versions if found.</param>
        /// <returns>True if versions were found for the location, false otherwise.</returns>
        bool TryGetStableVersions(string normalizedLocation, out ISet<string> versions);
    }
}