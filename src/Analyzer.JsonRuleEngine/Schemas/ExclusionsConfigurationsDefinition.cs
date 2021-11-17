// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
{
    /// <summary>
    /// The base class for all Exclusions schemas in JSON configurations.
    /// </summary>
    internal abstract class ExclusionsConfigurationsDefinition
    {
        /// <summary>
        /// Gets or sets the Severity property.
        /// </summary>
        [JsonProperty]
        public List<int> Severity { get; set; }

        /// <summary>
        /// Gets or sets the RuleIds property.
        /// </summary>
        [JsonProperty]
        public List<string> RuleIds { get; set; }
    }
}
