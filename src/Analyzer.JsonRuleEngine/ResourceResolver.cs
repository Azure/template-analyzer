// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine
{
    /// <summary>
    /// Resource Resolver is called to find the resources of the specified resource type
    /// </summary>
    internal class ResourceResolver
    {
        private readonly string resourceType;
        private readonly JToken template;
        
        /// <summary>
        /// Resources of the specified resource type
        /// </summary>
        public List<JToken> Resources { get; private set; }

        /// <summary>
        /// Creates an instance of ResourceResolver, used to get all the resources of a given type.
        /// </summary>
        /// <param name="resourceType">Resource type to find.</param>
        /// <param name="template">ARM template to get resources from.</param>
        public ResourceResolver(string resourceType, JToken template)
        {
            this.resourceType = resourceType;
            this.template = template;

            SetResources();
        }

        private void SetResources()
        {
            Resources = new List<JToken>();

            foreach (var resource in template.InsensitiveToken("resources"))
            {
                if (string.Equals(resource.InsensitiveToken("type").Value<string>(), resourceType, System.StringComparison.OrdinalIgnoreCase))
                {
                    Resources.Add(resource);
                }
            }
        }
    }
}