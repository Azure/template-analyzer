﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
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
        /// or 1 if it can't be determined.</returns>
        public int ResolveLineNumber(string pathInExpandedTemplate)
        {
            var rootTemplateContext = this.templateContext;
            while (rootTemplateContext != null && !rootTemplateContext.IsMainTemplate)
            {
                rootTemplateContext = rootTemplateContext.ParentContext;
            }

            if (rootTemplateContext == null)
            {
                throw new ArgumentNullException(nameof(rootTemplateContext));
            }
            if (!rootTemplateContext.IsMainTemplate)
            {
                throw new FileNotFoundException(nameof(rootTemplateContext));
            }

            JToken expandedTemplateRoot = rootTemplateContext.ExpandedTemplate;
            JToken originalTemplateRoot = rootTemplateContext.OriginalTemplate;

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
                return 1;
            }

            // Handle path and prefixes one level at a time to construct an accurate resources' path
            var currentContext = this.templateContext;
            var currentPathToEvaluate = pathInExpandedTemplate;
            string mappedPathInExpandedTemplate = "";
            while (currentContext != null)
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
                        return 1;
                    }

                    mappedPathInExpandedTemplate = $"{originalResourcePath}.{remainingPathAtResourceScope}.{mappedPathInExpandedTemplate}";
                }
                currentPathToEvaluate = currentContext.PathPrefix;
                currentContext = currentContext.ParentContext;
            }

            if (mappedPathInExpandedTemplate.Length > 0 && !mappedPathInExpandedTemplate.Equals(pathInExpandedTemplate))
            {
                tokenFromOriginalTemplate = originalTemplateRoot.InsensitiveToken(mappedPathInExpandedTemplate, InsensitivePathNotFoundBehavior.LastValid);
            }
            return (tokenFromOriginalTemplate as IJsonLineInfo)?.LineNumber ?? 1;
        }
    }
}