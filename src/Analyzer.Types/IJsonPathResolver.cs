// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// An interface for working with a JSON scope.
    /// </summary>
    public interface IJsonPathResolver
    {
        /// <summary>
        /// Gets the JToken of the resolver's scope.
        /// </summary>
        public JToken JToken { get; }

        /// <summary>
        /// Gets the JSON path of the resolver's current scope.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Retrieves the JToken(s) of the current scope at the specified path.
        /// </summary>
        /// <param name="jsonPath">JSON path to follow.</param>
        /// <returns>The JToken(s) at the path.</returns>
        public IEnumerable<IJsonPathResolver> Resolve(string jsonPath);

        /// <summary>
        /// Retrieves the JTokens for resources of the specified type
        /// in a "resources" property array at the current scope.
        /// </summary>
        /// <param name="resourceType">The type of resource to find.</param>
        /// <returns>An enumerable of resolvers with a scope of a resource of the specified type.</returns>
        public IEnumerable<IJsonPathResolver> ResolveResourceType(string resourceType);
    }
}