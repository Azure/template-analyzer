// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Converters;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas
{
    /// <summary>
    /// The base class for all Expression schemas in JSON rules.
    /// </summary>
    [JsonConverter(typeof(ExpressionConverter))]
    internal abstract class ExpressionDefinition
    {
        /// <summary>
        /// Gets or sets the Path property.
        /// </summary>
        [JsonProperty]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the ResourceType property.
        /// </summary>
        [JsonProperty]
        public string ResourceType { get; set; }

        /// <summary>
        /// Creates an <c>Expression</c> that can evaluate a template.
        /// </summary>
        /// <returns>The <c>Expression</c>.</returns>
        public abstract Expression ToExpression();
    }
}
