// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine
{
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
