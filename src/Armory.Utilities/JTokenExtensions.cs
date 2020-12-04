// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Armory.Utilities
{
    /// <summary>
    /// This class is used to simplify accessing values in JTokens.
    /// </summary>
    public static class JTokenExtensions
    {
        private static readonly Regex jArrayRegex = new Regex(@"^(?<property>\w+)\[(?<index>\d+)\]$", RegexOptions.Compiled);

        /// <summary>
        /// Finds a child JToken with the specified property name or JSON path.
        /// If a path is passed, each child in the path must be named and separated by '.' .
        /// JArray indexing is also supported.
        /// </summary>
        /// <param name="token">The JToken to search from</param>
        /// <param name="propertyNameOrPath">The property name or JSON path to search for</param>
        /// <returns>The named property, or the JToken at the end of the path, or null if not found.</returns>
        public static JToken InsensitiveToken(this JToken token, string propertyNameOrPath) =>
            InsensitiveToken(token, propertyNameOrPath, InsensitivePathNotFoundBehavior.Null);

        /// <summary>
        /// Finds a child JToken with the specified property name or JSON path.
        /// If a path is passed, each child in the path must be named and separated by '.' .
        /// JArray indexing is also supported.
        /// </summary>
        /// <param name="token">The JToken to search from.</param>
        /// <param name="propertyNameOrPath">The property name or JSON path to search for.</param>
        /// <param name="behaviorWhenNotFound">Behavior if property or path is not fully resolved.</param>
        /// <returns>The named property, or the JToken at the end of the path, if found.
        /// If not found, behavior is determined by <paramref name="behaviorWhenNotFound"/>.</returns>
        public static JToken InsensitiveToken(this JToken token, string propertyNameOrPath, InsensitivePathNotFoundBehavior behaviorWhenNotFound)
        {
            if (token == null)
            {
                switch (behaviorWhenNotFound)
                {
                    case InsensitivePathNotFoundBehavior.Error:
                        throw new ArgumentNullException(nameof(token));
                    case InsensitivePathNotFoundBehavior.LastValid:
                    case InsensitivePathNotFoundBehavior.Null:
                        return null;
                }
            }

            if (string.IsNullOrWhiteSpace(propertyNameOrPath))
            {
                switch (behaviorWhenNotFound)
                {
                    case InsensitivePathNotFoundBehavior.Null:
                        return null;
                    case InsensitivePathNotFoundBehavior.Error:
                        throw new ArgumentException($"{nameof(propertyNameOrPath)} is null or whitespace.");
                    default:
                        break;
                }
            }

            string[] properties = propertyNameOrPath?.Split('.') ?? new string[0];
            List<string> pathSoFar = new List<string>();

            foreach (var propertyName in properties)
            {
                pathSoFar.Add(propertyName);
                bool foundChild = false;
                Exception lastException = null;
                var arrayMatch = jArrayRegex.Match(propertyName);

                foreach (var child in token.Children())
                {
                    if (child is JProperty childProperty)
                    {
                        if (arrayMatch.Success && string.Equals(arrayMatch.Groups["property"].Value, childProperty.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                var index = int.Parse(arrayMatch.Groups["index"].Value);
                                token = childProperty.Value[index];
                                foundChild = true;
                            }
                            catch (Exception e)
                            {
                                lastException = e;
                            }
                            break;
                        }
                        else if (string.Equals(propertyName, childProperty.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            token = childProperty.Value;
                            foundChild = true;
                            break;
                        }
                    }
                }
                if (!foundChild)
                {
                    switch (behaviorWhenNotFound)
                    {
                        case InsensitivePathNotFoundBehavior.Error:
                            throw new Exception($"JSON path was unresolved at {string.Join(",", pathSoFar)}", lastException);
                        case InsensitivePathNotFoundBehavior.LastValid:
                            return token;
                        case InsensitivePathNotFoundBehavior.Null:
                            return null;
                    }
                }
            }

            return token;
        }
    }

    /// <summary>
    /// Used to control behavior of InsensitiveToken() extension when a property or path is not found.
    /// </summary>
    public enum InsensitivePathNotFoundBehavior
    {
        /// <summary>
        /// Returns null if the property is not found, or JSON path is not fully resolved.
        /// </summary>
        Null,

        /// <summary>
        /// Throws an exception if the property is not found, or JSON path is not fully resolved.
        /// </summary>
        Error,

        /// <summary>
        /// If the property is not found, or JSON path is not fully resolved, returns a reference to the last valid JToken in the path.
        /// </summary>
        LastValid
    }
}
