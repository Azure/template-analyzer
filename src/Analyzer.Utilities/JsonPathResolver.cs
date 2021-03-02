// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An <c>IJsonPathResolver</c> to resolve JSON paths.
    /// </summary>
    public class JsonPathResolver : IJsonPathResolver
    {
        private readonly JToken currentScope;
        private readonly string currentPath;
        private readonly Dictionary<string, IEnumerable<FieldContent>> resolvedPaths;

        /// <summary>
        /// Creates an instance of JsonPathResolver, used to resolve Json paths from a JToken.
        /// </summary>
        /// <param name="jToken">The starting JToken.</param>
        /// <param name="path">The path to the specified JToken.</param>
        public JsonPathResolver(JToken jToken, string path)
            : this(jToken, path, new Dictionary<string, IEnumerable<FieldContent>>(StringComparer.OrdinalIgnoreCase))
        {
            // Check for null here.
            // A null JToken is allowed when creating a new instance privately,
            // but it should never be null when first constructed publicly.
            if (jToken == null)
            {
                throw new ArgumentNullException(nameof(jToken));
            }

            this.resolvedPaths[this.currentPath] = new List<FieldContent> { new FieldContent { Value = this.currentScope } };
        }

        private JsonPathResolver(JToken jToken, string path, Dictionary<string, IEnumerable<FieldContent>> resolvedPaths)
        {
            this.currentScope = jToken;
            this.currentPath = path ?? throw new ArgumentNullException(nameof(path));
            this.resolvedPaths = resolvedPaths ?? throw new ArgumentNullException(nameof(resolvedPaths));
        }

        /// <summary>
        /// Retrieves the JToken(s) at the specified path from the current scope.
        /// </summary>
        /// <param name="jsonPath">JSON path to follow.</param>
        /// <returns>The JToken(s) at the path. If the path does not exist, returns a JToken with a null value.</returns>
        public IEnumerable<IJsonPathResolver> Resolve(string jsonPath)
        {
            string fullPath = string.IsNullOrEmpty(jsonPath) ? currentPath : string.Join('.', currentPath, jsonPath);

            if (!resolvedPaths.TryGetValue(fullPath, out var resolvedTokens))
            {
                resolvedTokens = new List<FieldContent> { this.currentScope.InsensitiveToken(jsonPath) };
                resolvedPaths[fullPath] = resolvedTokens;
            }

            foreach (var token in resolvedTokens)
            {
                // If token is not null, defer to it's path, since the path looked up could include wildcards.
                yield return new JsonPathResolver(token.Value, token?.Value?.Path ?? fullPath, this.resolvedPaths);
            }
        }

        /// <summary>
        /// Retrieves the JTokens for resources of the specified type
        /// in a "resources" property array at the current scope.
        /// </summary>
        /// <param name="resourceType">The type of resource to find.</param>
        /// <returns>An enumerable of resolvers with a scope of a resource of the specified type.</returns>
        public IEnumerable<IJsonPathResolver> ResolveResourceType(string resourceType)
        {
            string fullPath = currentPath + ".resources[*]";
            if (!resolvedPaths.TryGetValue(fullPath, out var resolvedTokens))
            {
                var resources = this.currentScope.InsensitiveTokens("resources[*]");
                resolvedTokens = resources.Select(r => (FieldContent)r.jtoken);
                resolvedPaths[fullPath] = resolvedTokens;
            }

            foreach (var resource in resolvedTokens)
            {
                if (string.Equals(resource.Value.InsensitiveToken("type")?.Value<string>(), resourceType, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new JsonPathResolver(resource.Value, resource.Value.Path, this.resolvedPaths);
                }
            }
        }

        /// <inheritdoc/>
        public JToken JToken => this.currentScope;

        /// <inheritdoc/>
        public string Path => this.currentPath;
    }
}
