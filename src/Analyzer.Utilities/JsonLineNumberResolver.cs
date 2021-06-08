// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An <see cref="ILineNumberResolver"/> used for resolving line numbers from an expanded JSON template to the original JSON template.
    /// </summary>
    public class JsonLineNumberResolver : ILineNumberResolver
    {
        private static string resourceIndexPattern = @"resources\[(?<index>\d+)\]";
        private static readonly Regex resourceIndexInPath = new Regex(resourceIndexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex childResourceIndexInPath = new Regex($"{resourceIndexPattern}.{resourceIndexPattern}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly TemplateContext templateContext;

        /// <summary>
        /// Create a new instance with the given <see cref="TemplateContext"/>.
        /// </summary>
        /// <param name="templateContext">The template context to map JSON paths against.</param>
        public JsonLineNumberResolver(TemplateContext templateContext)
        {
            this.templateContext = templateContext ?? throw new ArgumentNullException(nameof(templateContext));
        }

        /// <summary>
        /// Given a JSON path in an expanded JSON template, find the equivalent line number
        /// in the original JSON template.
        /// </summary>
        /// <param name="pathInExpandedTemplate">The path in the expanded template
        /// to find the line number of in the original template.</param>
        /// <returns>The line number of the equivalent location in the original template,
        /// or 0 if it can't be determined.</returns>
        public int ResolveLineNumber(string pathInExpandedTemplate)
        {
            JToken expandedTemplateRoot = this.templateContext.ExpandedTemplate;
            JToken originalTemplateRoot = this.templateContext.OriginalTemplate;
            Match match;

            if (pathInExpandedTemplate == null || originalTemplateRoot == null)
            {
                throw new ArgumentNullException(pathInExpandedTemplate == null
                    ? nameof(pathInExpandedTemplate)
                    : nameof(originalTemplateRoot));
            }

            // Attempt to find an equivalent JToken in the original template from the expanded template's path directly
            var tokenFromOriginalTemplate = originalTemplateRoot.InsensitiveToken(pathInExpandedTemplate, InsensitivePathNotFoundBehavior.LastValid);

            // If the JToken returned from looking up the expanded template path is
            // just pointing to the root of the original template, then
            // even the first property could not be found in the original template.
            if (tokenFromOriginalTemplate.Equals(originalTemplateRoot))
            {
                return 0;
            }

            // If the JToken returned from looking up the expanded template path is
            // pointing to the resources array of the original template, then it's likely
            // the expanded template path used an index too large for the original template.
            if (tokenFromOriginalTemplate.Equals(originalTemplateRoot.InsensitiveToken("resources")))
            {
                // Verify the path starts with indexing into the template resources[] array
                match = resourceIndexInPath.Match(pathInExpandedTemplate);
                if (match.Success)
                {
                    // Verify the expanded template is available.
                    // (Avoid throwing earlier since this is not always needed.)
                    if (expandedTemplateRoot == null)
                    {
                        throw new ArgumentNullException(nameof(expandedTemplateRoot));
                    }

                    // The first property is a resource, likely with a larger index
                    // than the number of resources in the original template.
                    // See if the requested resource is a copied resource.
                    var resourceWithIndex = match.Value;
                    var parsedResource = expandedTemplateRoot.InsensitiveToken($"{resourceWithIndex}.copy.name", InsensitivePathNotFoundBehavior.Null);
                    if (parsedResource != null)
                    {
                        // Find the resource with this copy name in the original template
                        bool foundResourceCopySource = false;
                        var copyName = parsedResource.Value<string>();
                        foreach (var resource in originalTemplateRoot.InsensitiveToken("resources").Children())
                        {
                            var thisResourceCopyName = resource.InsensitiveToken("copy.name")?.Value<string>();
                            if (string.Equals(copyName, thisResourceCopyName))
                            {
                                // This is the original resource for the expanded copy.
                                // Replace the index in the path with the index of the source copy.
                                pathInExpandedTemplate = resourceIndexInPath.Replace(pathInExpandedTemplate, resource.Path);
                                tokenFromOriginalTemplate = originalTemplateRoot.InsensitiveToken(pathInExpandedTemplate, InsensitivePathNotFoundBehavior.LastValid);
                                foundResourceCopySource = true;
                                break;
                            }
                        }

                        if (!foundResourceCopySource)
                        {
                            // Unknown issue - there's a copied resource without a matching original copy
                            return 0;
                        }
                    }
                    else
                    {
                        // Unknown issue - there's an extra resource that didn't come from a copy loop
                        return 0;
                    }
                }
            }

            var originalTemplatePath = tokenFromOriginalTemplate.Path;
            var unmatchedPathInOriginalTemplate = pathInExpandedTemplate[originalTemplatePath.Length..].TrimStart('.');

            bool pathsAreEqual = string.Equals(originalTemplatePath, pathInExpandedTemplate, StringComparison.OrdinalIgnoreCase);

            bool unmatchedSegmentReferencesResourcesArray = unmatchedPathInOriginalTemplate.StartsWith("resources", StringComparison.OrdinalIgnoreCase);
            bool unmatchedSegmentStartsWithIndexArray = unmatchedPathInOriginalTemplate.StartsWith("[", StringComparison.OrdinalIgnoreCase);
            bool originalPathEndsWithResourcesArray = originalTemplatePath.EndsWith("resources", StringComparison.OrdinalIgnoreCase);

            bool pathIsNotFromCopiedResource = !unmatchedSegmentReferencesResourcesArray
                    && !unmatchedSegmentStartsWithIndexArray
                    && !originalPathEndsWithResourcesArray;

            // Compare the path of the expanded template with the path of the JToken found in the original template.
            // If they match or if the path is not from a copied resource
            // the line number from the JToken of the original template can be returned directly.
            if (pathsAreEqual || pathIsNotFromCopiedResource)
            {
                return (tokenFromOriginalTemplate as IJsonLineInfo)?.LineNumber ?? 0;
            }

            // Get the line number of the original resource before it was copied
            int firstPeriodIndex = unmatchedPathInOriginalTemplate.IndexOf('.') + 1;
            string remainingPathAtResourceScope = unmatchedPathInOriginalTemplate[firstPeriodIndex..];

            match = childResourceIndexInPath.Match(pathInExpandedTemplate);
            if (match.Success)
            {
                // Verify the expanded template is available.
                // (Avoid throwing earlier since this is not always needed.)
                if (expandedTemplateRoot == null)
                {
                    throw new ArgumentNullException(nameof(expandedTemplateRoot));
                }

                var resourceWithIndex = match.Value;

                // Get the resource name and type from the expanded template
                var childResourceName = expandedTemplateRoot.InsensitiveToken($"{resourceWithIndex}.name", InsensitivePathNotFoundBehavior.Null);
                var childResourceType = expandedTemplateRoot.InsensitiveToken($"{resourceWithIndex}.type", InsensitivePathNotFoundBehavior.Null);
                if (childResourceName != null && childResourceType != null)
                {
                    // Find the resource with this name and type in the expanded template
                    var childName = childResourceName.Value<string>();
                    var childType = childResourceType.Value<string>();
                    foreach (var resource in expandedTemplateRoot.InsensitiveToken("resources").Children())
                    {
                        var thisResourceName = resource.InsensitiveToken("name")?.Value<string>();
                        var thisResourceType = resource.InsensitiveToken("type")?.Value<string>();
                        if (string.Equals(childType, thisResourceType, StringComparison.OrdinalIgnoreCase) && string.Equals(childName, thisResourceName, StringComparison.OrdinalIgnoreCase))
                        {
                            // This is the original resource of the child resource
                            return ResolveLineNumber($"{resource.Path}.{remainingPathAtResourceScope}");
                        }
                    }
                }
            }
            return 1;
        }
    }
}
