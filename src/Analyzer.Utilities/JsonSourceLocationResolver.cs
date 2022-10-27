// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An <see cref="ISourceLocationResolver"/> used for resolving line numbers from an expanded JSON template to the original JSON template.
    /// </summary>
    public class JsonSourceLocationResolver : ISourceLocationResolver
    {
        private static readonly Regex resourceIndexInPath = new Regex(@"resources\[(?<index>\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly TemplateContext templateContext;

        /// <summary>
        /// Create a new instance with the given <see cref="TemplateContext"/>.
        /// </summary>
        /// <param name="templateContext">The template context to map JSON paths against.</param>
        public JsonSourceLocationResolver(TemplateContext templateContext)
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
        /// or 1 if it can't be determined.</returns>
        public SourceLocation ResolveSourceLocation(string pathInExpandedTemplate)
        {
            if (pathInExpandedTemplate == null)
            {
                throw new ArgumentNullException(nameof(pathInExpandedTemplate));
            }

            var rootTemplateContext = this.templateContext;
            while (rootTemplateContext != null && !rootTemplateContext.IsMainTemplate)
            {
                rootTemplateContext = rootTemplateContext.ParentContext;
            }

            if (rootTemplateContext == null)
            {
                throw new Exception("Could not find the context of the root template");
            }

            JToken expandedTemplateRoot = rootTemplateContext.ExpandedTemplate;
            JToken originalTemplateRoot = rootTemplateContext.OriginalTemplate;

            if (originalTemplateRoot == null)
            {
                throw new ArgumentNullException(nameof(originalTemplateRoot));
            }

            // Handle path and prefixes backwards one level at a time to construct an accurate resources' path
            var currentContext = this.templateContext;
            var currentPathToEvaluate = pathInExpandedTemplate;
            string fullPathFromExpandedParentTemplate = string.Empty;

            while (currentContext != null && currentPathToEvaluate.Length > 0)
            {
                // If the path is in the resources array of the template
                var matches = resourceIndexInPath.Matches(currentPathToEvaluate);
                if (matches.Count > 0)
                {
                    // Get the path of the child resource in the expanded template
                    string resourceWithIndex = string.Join('.', matches);

                    // Verify the expanded template is available.
                    // (Avoid throwing earlier since this is not always needed.)
                    if (expandedTemplateRoot == null)
                    {
                        throw new ArgumentNullException(nameof(expandedTemplateRoot));
                    }

                    string remainingPathAtResourceScope = currentPathToEvaluate[(resourceWithIndex.Length + 1)..];

                    if (!currentContext.ResourceMappings.TryGetValue(resourceWithIndex, out string originalResourcePath))
                    {
                        return new SourceLocation(currentContext.TemplateIdentifier, 1);
                    }

                    fullPathFromExpandedParentTemplate = $"{originalResourcePath}.{remainingPathAtResourceScope}.{fullPathFromExpandedParentTemplate}";
                }
                else
                {
                    fullPathFromExpandedParentTemplate = $"{currentPathToEvaluate}.{fullPathFromExpandedParentTemplate}";
                }
                currentPathToEvaluate = currentContext.PathPrefix;
                currentContext = currentContext.ParentContext;
            }

            JToken tokenFromOriginalTemplate = originalTemplateRoot.InsensitiveToken(fullPathFromExpandedParentTemplate, InsensitivePathNotFoundBehavior.LastValid);

            return new SourceLocation(
                this.templateContext.TemplateIdentifier,
                (tokenFromOriginalTemplate as IJsonLineInfo)?.LineNumber ?? 1);
        }
    }
}