// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Armory.Utilities;
using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine
{
    /// <summary>
    /// An <c>IJsonPathResolver</c> to resolve JSON paths.
    /// </summary>
    internal class JsonPathResolver : IJsonPathResolver
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
            this.resolvedPaths[this.currentPath] = new List<FieldContent> { new FieldContent { Value = this.currentScope } };
        }

        private JsonPathResolver(JToken jToken, string path, Dictionary<string, IEnumerable<FieldContent>> resolvedPaths)
        {
            (this.currentScope, this.currentPath, this.resolvedPaths) = (jToken, path, resolvedPaths);
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
                resolvedTokens = new List<FieldContent> { new FieldContent { Value = this.currentScope.InsensitiveToken(jsonPath) } };
                resolvedPaths[fullPath] = resolvedTokens;
            }

            foreach (var token in resolvedTokens)
            {
                // If token is not null, defer to it's path, since the path looked up could include wildcards.
                yield return new JsonPathResolver(token.Value, token?.Value?.Path ?? fullPath, this.resolvedPaths);
            }
        }

        /// <summary>
        /// The JToken in scope of this resolver.
        /// </summary>
        public JToken JToken => this.currentScope;
    }
}
