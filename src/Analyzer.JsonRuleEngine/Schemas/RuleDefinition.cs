// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
{
    /// <summary>
    /// Represents a rule written in JSON.
    /// </summary>
    internal class RuleDefinition
    {
        /// <summary>
        /// Gets or sets the id of the rule.
        /// </summary>
        [JsonProperty]
        public string Id { get; set; }

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
        /// Gets or sets the severity of the rule. 
        /// </summary> 
        [JsonProperty] 
        public Severity Severity { get; set; } = Severity.Medium;

        /// <summary>
        /// Gets or sets the expression details of the rule.
        /// </summary>
        [JsonProperty(PropertyName = "evaluation")]
        public ExpressionDefinition ExpressionDefinition { get; set; }

        /// <summary>
        /// Gets or sets an <see cref="Expression"/> that can evaluate a template against this rule.
        /// </summary>
        [JsonIgnore]
        internal Expression Expression { get; set; }
    }
}
