// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Bicep.Core.Emit;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An <see cref="ISourceLocationResolver"/> used for resolving line numbers from a compiled JSON template to the original Bicep template.
    /// </summary>
    public class BicepSourceLocationResolver : ISourceLocationResolver
    {
        private readonly JsonSourceLocationResolver jsonLineNumberResolver;
        private readonly SourceMap sourceMap;

        /// <summary>
        /// Create a new instance with the given <see cref="TemplateContext"/>.
        /// </summary>
        /// <param name="templateContext">The template context to map JSON paths against.</param>
        public BicepSourceLocationResolver(TemplateContext templateContext)
        {
            this.jsonLineNumberResolver = new(templateContext ?? throw new ArgumentNullException(nameof(templateContext)));
            this.sourceMap = (templateContext.SourceMap as SourceMap) ?? throw new ArgumentNullException(nameof(templateContext.SourceMap));
        }

        /// <summary>
        /// Given a JSON path in an expanded JSON template from a compiled Bicep file, find the equivalent line number
        /// in the original Bicep template.
        /// </summary>
        /// <param name="pathInExpandedTemplate">The path in the expanded template
        /// to find the line number of in the original template.</param>
        /// <returns>The line number of the equivalent location in the original template,
        /// or 1 if it can't be determined.</returns>
        public SourceLocation ResolveSourceLocation(string pathInExpandedTemplate)
        {
            var jsonLine = this.jsonLineNumberResolver.ResolveSourceLocation(pathInExpandedTemplate).LineNumber;

            // Source map line numbers from Bicep are 0-indexed
            jsonLine--;

            // search each source file for matching mapping, picking the most specific match (source line maps to least amount of target lines)

            SourceMapEntry bestMatch = null;
            string bestMatchSourceFile = null;
            int bestMatchSize = int.MaxValue;
            foreach(var fileEntry in sourceMap.Entries)
            {
                var match = fileEntry.SourceMap.FirstOrDefault(mapping => mapping.TargetLine == jsonLine);
                if (match == default) continue;

                var matchSize = fileEntry.SourceMap.Count(mapping => mapping.SourceLine == match.SourceLine);

                if (matchSize < bestMatchSize)
                {
                    bestMatch = match;
                    bestMatchSourceFile = fileEntry.FilePath;
                    bestMatchSize = matchSize;
                }
            }

            return (bestMatch != null)
                ? new SourceLocation(
                    bestMatch.SourceLine + 1, // convert to 1-indexing
                    (bestMatchSourceFile != sourceMap.Entrypoint) ? bestMatchSourceFile : default) // show source file if not in entrypoint file
                : new SourceLocation(1);
        }
    }
}