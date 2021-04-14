// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// This class is used to simplify accessing values in JTokens.
    /// </summary>
    public static class JTokenExtensions
    {
        private static readonly Regex jArrayRegex = new Regex(@"^(?<property>\w+)\[(?<index>\d+|\*)\]$", RegexOptions.Compiled);

        /// <summary>
        /// Helper class for InsensitiveTokens() method.
        /// Instances of this class are added to a queue, and as items
        /// are dequeued, the next element in the path is searched for
        /// in the context of the item.
        /// </summary>
        private class InsensitiveTokenContext
        {
            /// <summary>
            /// The current token found in this context.
            /// </summary>
            public JToken Token;

            /// <summary>
            /// The path segments resolved so far up to <see cref="Token"/>.
            /// </summary>
            public List<string> ResolvedPath;

            /// <summary>
            /// The next index of the path segments to look for.
            /// (The path segments are looked up in <see cref="InsensitiveTokens(JToken, string, InsensitivePathNotFoundBehavior)"/>.
            /// </summary>
            public int NextPathIndex;
        }

        /// <summary>
        /// Finds a child JToken with the specified property name or JSON path.
        /// If a path is passed, each child in the path must be separated by '.'.
        /// JArray integer indexing is also supported.
        /// </summary>
        /// <param name="token">The JToken to search from</param>
        /// <param name="propertyNameOrPath">The property name or JSON path to search for</param>
        /// <returns>The named property, or the JToken at the end of the path, or null if not found.</returns>
        [DebuggerStepThrough]
        public static JToken InsensitiveToken(this JToken token, string propertyNameOrPath) =>
            InsensitiveToken(token, propertyNameOrPath, InsensitivePathNotFoundBehavior.Null);

        /// <summary>
        /// Finds a child JToken with the specified property name or JSON path.
        /// If a path is passed, each child in the path must be separated by '.'.
        /// JArray integer indexing is also supported.
        /// </summary>
        /// <param name="token">The JToken to search from.</param>
        /// <param name="propertyNameOrPath">The property name or JSON path to search for.</param>
        /// <param name="behaviorWhenNotFound">Behavior if property or path is not fully resolved.</param>
        /// <returns>The named property, or the JToken at the end of the path, if found.
        /// If not found, behavior is determined by <paramref name="behaviorWhenNotFound"/>.</returns>
        [DebuggerStepThrough]
        public static JToken InsensitiveToken(this JToken token, string propertyNameOrPath, InsensitivePathNotFoundBehavior behaviorWhenNotFound) =>
            InsensitiveTokens(token, propertyNameOrPath, behaviorWhenNotFound).FirstOrDefault();

        /// <summary>
        /// Finds child JTokens at the specified JSON path. Each child in the path
        /// must be separated by '.'.  To select all children under an element, use
        /// '*' as a wildcard.  '*' must appear by itself with no other characters
        /// between the '.' characters in the path. JArray integer and '*' wildcard
        /// indexing are also supported.
        /// </summary>
        /// <param name="token">The JToken to search from.</param>
        /// <param name="path">The JSON path to search for.</param>
        /// <returns>
        /// A JToken for each fully resolved property found for the requested path.
        /// Many tokens could be returned if wildcards are present in the path.
        /// If the path cannot be completely resolved, null will be returned.
        /// </returns>
        [DebuggerStepThrough]
        public static IEnumerable<JToken> InsensitiveTokens(this JToken token, string path) =>
            InsensitiveTokens(token, path, InsensitivePathNotFoundBehavior.Null);

        /// <summary>
        /// Finds child JTokens at the specified JSON path. Each child in the path
        /// must be separated by '.'.  To select all children under an element, use
        /// '*' as a wildcard.  '*' must appear by itself with no other characters
        /// between the '.' characters in the path. JArray integer and '*' wildcard
        /// indexing are also supported.
        /// </summary>
        /// <param name="token">The JToken to search from.</param>
        /// <param name="path">The JSON path to search for.</param>
        /// <param name="behaviorWhenNotFound">Behavior if path is not fully resolved.
        /// If <see cref="InsensitivePathNotFoundBehavior.Null"/> is specified, and the
        /// path contains wildcards, only JTokens which fully match the specified path
        /// are returned; all other path 'branches' are discarded.</param>
        /// <returns>
        /// A JToken for each fully resolved property found for the requested path.
        /// Many tokens could be returned if wildcards are present in the path.
        /// If the path cannot be completely resolved, the JToken returned will be
        /// determined by <paramref name="behaviorWhenNotFound"/>.  If there are no wildcards
        /// in the path, and <paramref name="behaviorWhenNotFound"/> is <see cref="InsensitivePathNotFoundBehavior.Null"/>,
        /// and the path was not resolved, a single null is returned.
        /// </returns>
        public static IEnumerable<JToken> InsensitiveTokens(this JToken token, string path, InsensitivePathNotFoundBehavior behaviorWhenNotFound)
        {
            // Verify a valid behavior is passed
            if (!Enum.IsDefined(typeof(InsensitivePathNotFoundBehavior), behaviorWhenNotFound))
            {
                throw new ArgumentException("Value passed is not defined.", nameof(behaviorWhenNotFound));
            }

            bool pathContainsWildcard = path != null && path.Contains('*');

            if (token == null)
            {
                switch (behaviorWhenNotFound)
                {
                    case InsensitivePathNotFoundBehavior.Error:
                        throw new ArgumentNullException(nameof(token));
                    case InsensitivePathNotFoundBehavior.LastValid:
                        // return null here, since there is no other 'last valid' object to return
                        yield return null;
                        yield break;
                    case InsensitivePathNotFoundBehavior.Null:
                        // Return a single null if there are no wildcards.  Otherwise, don't return anything.
                        if (!pathContainsWildcard) yield return null;
                        yield break;
                };
            }

            if (path == null)
            {
                switch (behaviorWhenNotFound)
                {
                    case InsensitivePathNotFoundBehavior.Error:
                        throw new ArgumentException($"{nameof(path)} is null.");
                    case InsensitivePathNotFoundBehavior.LastValid:
                        yield return token;
                        yield break;
                    case InsensitivePathNotFoundBehavior.Null:
                        // Since path is null, there are no wildcards, so return a single null.
                        yield return null;
                        yield break;
                };
            }

            string[] properties = path.Split('.');

            var jtokensToSearchFrom = new Queue<InsensitiveTokenContext>();
            jtokensToSearchFrom.Enqueue(new InsensitiveTokenContext { Token = token, ResolvedPath = new List<string>(), NextPathIndex = 0 });

            bool fullPathFound = false;

            while (jtokensToSearchFrom.Count > 0)
            {
                var tokenContext = jtokensToSearchFrom.Dequeue();

                if (tokenContext.NextPathIndex == properties.Length)
                {
                    // Reached the end of the path.  Return current JToken.
                    yield return tokenContext.Token;
                    fullPathFound = true;
                    continue;
                }

                var propertyToFind = properties[tokenContext.NextPathIndex];

                if (string.Equals(propertyToFind, "*"))
                {
                    // Wildcard - add all children.
                    AddAllChildrenToQueue(jtokensToSearchFrom, tokenContext);
                    continue;
                }

                // See if property in requested path is indexing an array
                var arrayMatch = jArrayRegex.Match(propertyToFind);
                if (arrayMatch.Success)
                {
                    propertyToFind = arrayMatch.Groups["property"].Value;
                }

                bool foundChild = false;
                Exception lastException = null;
                foreach (var child in tokenContext.Token.Children())
                {
                    if (child is JProperty childProperty)
                    {
                        if (string.Equals(propertyToFind, childProperty.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            // Found the target property.
                            foundChild = true;
                            if (arrayMatch.Success)
                            {
                                // Path is indexing into this array property.
                                // Attempt to find the requested index.
                                if (childProperty.Value is JArray childArray)
                                {
                                    var index = arrayMatch.Groups["index"].Value;
                                    if (index == "*")
                                    {
                                        // Wildcard index - add all array elements and move to next path segment.
                                        AddAllJArrayElementsToQueue(jtokensToSearchFrom, childArray, tokenContext);
                                    }
                                    else
                                    {
                                        try
                                        {
                                            var intIndex = int.Parse(index);

                                            if (intIndex < childArray.Count)
                                            {
                                                // If index is in-bounds, select that index.
                                                EnqueueChildJToken(jtokensToSearchFrom, childArray[intIndex], tokenContext); 
                                            }
                                            else
                                            {
                                                // Otherwise, set the context to the array itself
                                                // and set found to false so searching stops.
                                                tokenContext.Token = childArray;
                                                foundChild = false;
                                            }

                                        }
                                        catch (Exception e)
                                        {
                                            lastException = e;
                                            foundChild = false;
                                        }
                                    }
                                }
                                else
                                {
                                    // Path indexed into a property that isn't an array.
                                    // Update current context to this child and change
                                    // found to false so searching stops.
                                    tokenContext.Token = childProperty.Value;
                                    foundChild = false;
                                }
                            }
                            else
                            {
                                EnqueueChildJToken(jtokensToSearchFrom, childProperty.Value, tokenContext);
                            }
                            break;
                        }
                    }
                }

                if (!foundChild)
                {
                    switch (behaviorWhenNotFound)
                    {
                        case InsensitivePathNotFoundBehavior.Error:
                            throw new Exception($"JSON path was unresolved at {string.Join(".", tokenContext.ResolvedPath)}", lastException);
                        case InsensitivePathNotFoundBehavior.LastValid:
                            yield return tokenContext.Token;
                            break;

                        // Paths not fully resolved are ignored for InsensitivePathNotFoundBehavior.Null.
                        // If the path doesn't contain wildcards, a single null may be returned below.
                    };
                }
            }

            // Return null if full, non-wildcard path is not found
            // and behaviorWhenNotFound is Null
            if (behaviorWhenNotFound == InsensitivePathNotFoundBehavior.Null
                && !(pathContainsWildcard || fullPathFound))
            {
                yield return null;
            }
        }

        /// <summary>
        /// Helper method for InsensitiveTokens().
        /// Adds all child properties of the token context to the queue to be processed later.
        /// For any child property which has a value of an array, all elements of the array
        /// are added to the queue.
        /// </summary>
        private static void AddAllChildrenToQueue(Queue<InsensitiveTokenContext> tokenQueue, InsensitiveTokenContext tokenContext)
        {
            foreach (var child in tokenContext.Token.Children())
            {
                if (child is JProperty childProperty)
                {
                    if (childProperty.Value is JArray jArray)
                    {
                        AddAllJArrayElementsToQueue(tokenQueue, jArray, tokenContext);
                        continue;
                    }

                    EnqueueChildJToken(tokenQueue, childProperty.Value, tokenContext);
                }
            }
        }

        /// <summary>
        /// Helper for InsensitiveTokens().
        /// Adds all elements of a JArray to the queue to be processed later.
        /// </summary>
        private static void AddAllJArrayElementsToQueue(Queue<InsensitiveTokenContext> tokenQueue, JArray jArray, InsensitiveTokenContext parentContext)
        {
            foreach (var elem in jArray)
            {
                EnqueueChildJToken(tokenQueue, elem, parentContext);
            }
        }

        /// <summary>
        /// Helper for InsensitiveTokens().
        /// Adds a new child JToken to the queue to be processed later.
        /// </summary>
        private static void EnqueueChildJToken(Queue<InsensitiveTokenContext> tokenQueue, JToken childToken, InsensitiveTokenContext parentContext)
        {
            // This method of getting the child path ensures that the correct array index
            // will be included in the case the parent property is an array.
            var propertyName = childToken.Path[(childToken.Path.LastIndexOf('.') + 1)..];
            var pathToChild = new List<string>(parentContext.ResolvedPath) { propertyName };
            tokenQueue.Enqueue(
                new InsensitiveTokenContext
                {
                    Token = childToken,
                    NextPathIndex = parentContext.NextPathIndex + 1,
                    ResolvedPath = pathToChild
                });
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
