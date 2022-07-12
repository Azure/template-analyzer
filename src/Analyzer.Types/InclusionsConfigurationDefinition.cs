// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// The inclusions defined in a <see cref="ConfigurationDefinition"/>.
    /// </summary>
    public class InclusionsConfigurationDefinition
    {
        /// <summary>
        /// Gets or sets the Severity property.
        /// </summary>
        [JsonProperty]
        public List<Severity> Severity { get; set; }

        /// <summary>
        /// Gets or sets the Ids property.
        /// </summary>
        [JsonProperty]
        public List<string> Ids { get; set; }
    }
}
