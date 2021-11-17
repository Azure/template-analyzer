// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
{
    /// <summary>
    /// Represents a configurations file written in JSON.
    /// </summary>
    internal class ConfigurationsDefinition
    {
        /// <summary>
        /// Gets or sets the exclusion details of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "exclusions")]
        public ExclusionsConfigurationsDefinition ExclusionsConfigurationsDefinition { get; set; }

        /// <summary>
        /// Gets or sets the inclusion details of the configuration.
        /// </summary>
        [JsonProperty(PropertyName = "inclusions")]
        public InclusionsConfigurationsDefinition InclusionsConfigurationsDefinition { get; set; }

        /// <summary>
        /// Gets or sets the severity remapping of the configuration
        /// </summary>
        public String SeverityReMapping { get; set; }
    }
}
