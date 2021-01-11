// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine
{
    /// <summary>
    /// Represents a rule written in JSON.
    /// </summary>
    internal class RuleDefinition
    {
        /// <summary>
        /// Gets or sets the name of the rule.
        /// </summary>
        [JsonProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the rule.
        /// </summary>
        [JsonProperty]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the recommendation of the rule.
        /// </summary>
        [JsonProperty]
        public string Recommendation { get; set; }

        /// <summary>
        /// Gets or sets the help uri of the rule.
        /// </summary>
        [JsonProperty]
        public string HelpUri { get; set; }

        /// <summary>
        /// Gets or sets the expression details of the rule.
        /// </summary>
        [JsonProperty]
        public ExpressionDefinition Evaluation { get; set; }
    }
}
