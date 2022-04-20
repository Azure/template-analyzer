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
        private ExpressionCommonProperties commonProperties = null;

        /// <summary>
        /// Gets or sets the Path property.
        /// </summary>
        [JsonProperty]
        public virtual string Path { get; set; }

        /// <summary>
        /// Gets or sets the ResourceType property.
        /// </summary>
        [JsonProperty]
        public string ResourceType { get; set; }

        /// <summary>
        ///  Gets or sets the Where property.
        /// </summary>
        [JsonProperty]
        public ExpressionDefinition Where { get; set; }

        /// <summary>
        /// Creates an <see cref="Expression"/> that can evaluate a template.
        /// </summary>
        /// <param name="isNegative">Whether to negate the evaluation.</param>
        /// <returns>The <see cref="Expression"/>.</returns>
        public abstract Expression ToExpression(bool isNegative = false);

        /// <summary>
        /// Gets the properties common across all <see cref="Expression"/> types.
        /// </summary>
        /// <returns>The common properties of the <see cref="Expression"/>.</returns>
        protected ExpressionCommonProperties CommonProperties =>
            commonProperties ??= new ExpressionCommonProperties
            {
                ResourceType = ResourceType,
                Path = Path,
                Where = Where?.ToExpression(isNegative: false)
            };

        /// <summary>
        /// Validates the ExpressionDefinition for valid syntax
        /// </summary>
        internal abstract void Validate();
    }
}
