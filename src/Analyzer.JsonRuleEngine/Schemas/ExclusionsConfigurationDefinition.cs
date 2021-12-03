// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
{
    /// <summary>
    /// The base class for all Exclusions schemas in JSON configuration.
    /// </summary>
    internal class ExclusionsConfigurationDefinition
    {
        /// <summary>
        /// Gets or sets the Severity property.
        /// </summary>
        [JsonProperty]
        public List<int> Severity { get; set; }

        /// <summary>
        /// Gets or sets the Ids property.
        /// </summary>
        [JsonProperty]
        public List<string> Ids { get; set; }
    }
}
