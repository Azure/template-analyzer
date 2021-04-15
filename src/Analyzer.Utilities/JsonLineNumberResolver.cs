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
        private static readonly Regex resourceIndexInPath = new Regex(@"resources\[(?<index>\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
