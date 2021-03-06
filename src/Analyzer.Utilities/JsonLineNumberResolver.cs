﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An <c>ILineNumberResolver</c> used for resolving line numbers from an expanded JSON template to the original JSON template.
    /// </summary>
    public class JsonLineNumberResolver : IJsonLineNumberResolver
    {
        private static readonly Regex resourceIndexInPath = new Regex(@"resources\[(?<index>\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <inheritdoc/>
        public int ResolveLineNumberForOriginalTemplate(
            string pathInExpandedTemplate,
            JToken expandedTemplateRoot,
            JToken originalTemplateRoot)
        {
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
                // See if the path starts with indexing into the template resources[] array
                var match = resourceIndexInPath.Match(pathInExpandedTemplate);
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

            // Compare the path of the expanded template with the path of the JToken found in the original template.
            // If they match, or if the first unmatched property in the original is NOT a resources array,
            // the line number from the JToken of the original template can be returned directly.
            if (originalTemplatePath.Length == pathInExpandedTemplate.Length
                || !unmatchedPathInOriginalTemplate.StartsWith("resources", StringComparison.OrdinalIgnoreCase))
            {
                return (tokenFromOriginalTemplate as IJsonLineInfo)?.LineNumber ?? 0;
            }

            // The first unmatched property must be a resources[] array, likely a subresource
            // of a top-level resource.  This is possible if resources were copied into other
            // resources as part of template expansion.
            // TODO
            return 1;
        }
    }
}
