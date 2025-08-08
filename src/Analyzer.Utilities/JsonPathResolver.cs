// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            string fullPath = CombineJsonPaths(currentPath, jsonPath);

            if (!resolvedPaths.TryGetValue(fullPath, out var resolvedTokens))
            {
                resolvedTokens = this.currentScope.InsensitiveTokens(jsonPath).Select(t => (FieldContent)t).ToList();
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
        /// in a "resources" property array or object at the current scope.
        /// Supports both traditional array format and language version 2.0 object format (symbolic naming).
        /// </summary>
        /// <param name="resourceType">The type of resource to find.</param>
        /// <returns>An enumerable of resolvers with a scope of a resource of the specified type.</returns>
        public IEnumerable<IJsonPathResolver> ResolveResourceType(string resourceType)
        {
            // Traditional ARM JSON templates use an array for resources,
            // while language version 2.0 uses an object.
            string arrayPath = "resources[*]";
            string objectPath = "resources.*";

            if (!resolvedPaths.TryGetValue(CombineJsonPaths(this.currentPath, arrayPath), out var resolvedTokens) &&
                !resolvedPaths.TryGetValue(CombineJsonPaths(this.currentPath, objectPath), out resolvedTokens))
            {
                string resourcesPath = this.currentScope.InsensitiveToken("resources") is JObject ? objectPath : arrayPath;
                string fullPath = CombineJsonPaths(this.currentPath, resourcesPath);

                var resources = this.currentScope.InsensitiveTokens(resourcesPath);
                resolvedTokens = resources.Select(r => (FieldContent)r).ToList();
                resolvedPaths[fullPath] = resolvedTokens;
            }

            // When trying to resolve Microsoft.Web/sites/siteextensions, for example, we should consider that siteextensions can be a child resource of Microsoft.Web/sites (a parent of the original resource type)
            var resourceTypeParents = new List<string> { };
            var indexesOfTypesSeparators = Regex.Matches(resourceType, "/").Select(m => m.Index).Skip(1);
            foreach (var indexOfTypeSeparator in indexesOfTypesSeparators)
            {
                resourceTypeParents.Add(resourceType[..indexOfTypeSeparator]);
            }

            static bool resourceTypesAreEqual(FieldContent jTokenResourceType, string stringResourceType)
            {
                return string.Equals(jTokenResourceType.Value.InsensitiveToken("type")?.Value<string>(), stringResourceType, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var resource in resolvedTokens)
            {
                if (resourceTypesAreEqual(resource, resourceType))
                {
                    yield return new JsonPathResolver(resource.Value, resource.Value.Path, this.resolvedPaths);
                }
                else if (resourceTypeParents.Exists(parentResourceType => resourceTypesAreEqual(resource.Value, parentResourceType)))
                {
                    // In this case we still haven't matched the whole resource type
                    var subScope = new JsonPathResolver(resource.Value, resource.Value.Path, this.resolvedPaths);
                    foreach (var newJsonPathResolver in subScope.ResolveResourceType(resourceType))
                    {
                        yield return newJsonPathResolver;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public JToken JToken => this.currentScope;

        /// <inheritdoc/>
        public string Path => this.currentPath;
    
        private string CombineJsonPaths(params string[] paths) =>
            paths == null
                ? string.Empty
                : string.Join(".", paths.Where(p => !string.IsNullOrEmpty(p)));
    }
}
