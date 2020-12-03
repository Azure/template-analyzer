// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Armory.Utilities;
using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine
{
    /// <summary>
    /// An <c>IJsonPathResolver</c> to resolve JSON paths
    /// </summary>
    internal class JsonPathResolver : IJsonPathResolver
    {
        private readonly JToken currentScope;
        private readonly string currentPath;
        private Dictionary<string, IEnumerable<JToken>> resolvedPaths;

        /// <summary>
        /// Creates an instance of JsonPathResolver, used to resolve Json paths from a JToken.
        /// </summary>
        /// <param name="jToken">The starting JToken</param>
        public JsonPathResolver(JToken jToken, string path)
            : this(jToken, path, new Dictionary<string, IEnumerable<JToken>>(StringComparer.OrdinalIgnoreCase))
        {
        }

        private JsonPathResolver(JToken scope, string currentPath, Dictionary<string, IEnumerable<JToken>> resolvedPaths)
        {
            (this.currentScope, this.currentPath, this.resolvedPaths) = (scope, currentPath, resolvedPaths);
        }

        /// <summary>
        /// Retrieves the JToken(s) of the current scope at the specified path
        /// </summary>
        /// <param name="jsonPath">JSON path to follow</param>
        /// <returns>The JToken(s) at the path. If the path does not exist, returns a JToken with a null value.</returns>
        public IEnumerable<IJsonPathResolver> Resolve(string jsonPath)
        {
            string fullPath = string.Join('.', currentPath, jsonPath);

            if (!resolvedPaths.TryGetValue(fullPath, out var resolvedTokens))
            {
                resolvedTokens = new List<JToken> { this.currentScope.InsensitiveToken(jsonPath) };
                resolvedPaths[fullPath] = resolvedTokens;
            }

            foreach (var token in resolvedTokens)
            {
                // If token is not null, defer to it's path, since the path looked up could include wildcards.
                yield return new JsonPathResolver(token, token?.Path ?? fullPath, this.resolvedPaths);
            }
        }

        public JToken JToken => this.currentScope;
    }

    /// <summary>
    /// A utility interface for resolving a JSON path in a JSON scope
    /// </summary>
    internal interface IJsonPathResolver
    {
        /// <summary>
        /// Gets the JToken of the resolver's scope
        /// </summary>
        public JToken JToken { get; }

        /// <summary>
        /// Retrieves the JToken(s) of the current scope at the specified path
        /// </summary>
        /// <param name="jsonPath">JSON path to follow</param>
        /// <returns>The JToken(s) at the path</returns>
        public IEnumerable<IJsonPathResolver> Resolve(string jsonPath);
    }
}
