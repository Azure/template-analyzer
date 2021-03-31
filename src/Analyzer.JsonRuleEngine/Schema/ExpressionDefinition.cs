// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Converters;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Utilities;
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
        /// <param name="jsonLineNumberResolver">An <see cref=" ILineNumberResolver"/> to
        /// pass to the created <see cref="Expression"/>.</param>
        /// <returns>The <see cref="Expression"/>.</returns>
        public abstract Expression ToExpression(ILineNumberResolver jsonLineNumberResolver);

        /// <summary>
        /// Gets the properties common across all <see cref="Expression"/> types.
        /// </summary>
        /// <param name="jsonLineNumberResolver">An <see cref=" ILineNumberResolver"/> to
        /// pass to any sub-<see cref="Expression"/>s created.</param>
        /// <returns>The common properties of the <see cref="Expression"/>.</returns>
        protected ExpressionCommonProperties GetCommonProperties(ILineNumberResolver jsonLineNumberResolver) => commonProperties
            ??= new ExpressionCommonProperties
            {
                ResourceType = ResourceType,
                Path = Path,
                Where = Where?.ToExpression(jsonLineNumberResolver)
            };

        /// <summary>
        /// Validates the ExpressionDefinition for valid syntax
        /// </summary>
        internal abstract void Validate();
    }
}
