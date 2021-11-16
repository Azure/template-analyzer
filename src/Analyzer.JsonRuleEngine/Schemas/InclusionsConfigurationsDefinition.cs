// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
{
    class InclusionsConfigurationsDefinition
    {
        /// <summary>
        /// Gets or sets the RuleIds property.
        /// </summary>
        [JsonProperty]
        public List<string> RuleIds { get; set; }
    }
}
