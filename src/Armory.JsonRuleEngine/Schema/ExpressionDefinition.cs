// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Armory.JsonRuleEngine
{
    [JsonConverter(typeof(ExpressionConverter))]
    internal abstract class ExpressionDefinition
    {
        /// <summary>
        /// Gets or sets the Path property
        /// </summary>
        [JsonProperty]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the ResourceType property
        /// </summary>
        [JsonProperty]
        public string ResourceType { get; set; }

        public abstract Expression ToExpression();
    }
}
