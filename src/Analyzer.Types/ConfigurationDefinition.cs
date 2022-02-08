// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Represents a configuration file written in JSON.
    /// * severityOverride is not yet implemented.
    /// </summary>
    public class ConfigurationDefinition
    {
        /// <summary>
        /// Gets or sets the exclusion details of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "exclusions")]
        public ExclusionsConfigurationDefinition ExclusionsConfigurationDefinition { get; set; }

        /// <summary>
        /// Gets or sets the inclusion details of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "inclusions")]
        public InclusionsConfigurationDefinition InclusionsConfigurationDefinition { get; set; }
    }
}
