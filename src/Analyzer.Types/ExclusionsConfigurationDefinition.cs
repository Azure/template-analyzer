// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Gets or sets the ExclusionsConfigurationDefinition.
    /// </summary>
    public class ExclusionsConfigurationDefinition
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
