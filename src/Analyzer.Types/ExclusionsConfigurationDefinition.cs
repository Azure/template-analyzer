// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;
using static Microsoft.Azure.Templates.Analyzer.Types.IEvaluation;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// The exclusions defined in a <see cref="ConfigurationDefinition"/>.
    /// </summary>
    public class ExclusionsConfigurationDefinition
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
