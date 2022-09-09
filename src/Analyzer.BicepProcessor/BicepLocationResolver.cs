// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Azure.Identity;
using Bicep.Core.Emit;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An <see cref="ISourceLocationResolver"/> used for resolving line numbers from a compiled JSON template to the original Bicep template.
    /// </summary>
    public class BicepSourceLocationResolver : ISourceLocationResolver
    {
        private readonly string EntrypointFilePath;
        private readonly JsonSourceLocationResolver jsonLineNumberResolver;
        private readonly SourceMap sourceMap;
        private readonly TemplateContext templateContext;

        /// <summary>
        /// Create a new instance with the given <see cref="TemplateContext"/>.
        /// </summary>
        /// <param name="templateContext">The template context to map JSON paths against.</param>
        public BicepSourceLocationResolver(TemplateContext templateContext)
        {
            this.templateContext = templateContext;
            this.EntrypointFilePath = (templateContext ?? throw new ArgumentNullException(nameof(templateContext))).TemplateIdentifier;
            this.jsonLineNumberResolver = new(templateContext);
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

            // find all files with match, record line number, file name, and map size (how many other target lines the source line maps to)
            var matches = new List<(int lineNumber, string filePathRelativeToEntrypoint, int mapSize)>();
            foreach (var fileEntry in sourceMap.Entries)
            {
                var match = fileEntry.SourceMap.FirstOrDefault(mapping => mapping.TargetLine == jsonLine);

                if (match != default)
                {
                    var matchSize = fileEntry.SourceMap.Count(mapping => mapping.SourceLine == match.SourceLine);
                    matches.Add((match.SourceLine, fileEntry.FilePath, matchSize)); 
                }
            }

            // sort largest to smallest map size to sort into reference order
            matches.Sort((x, y) => x.mapSize.CompareTo(y.mapSize));

            // default to result from JSON if no matches (i.e. a bicep module references a JSON template that wouldn't be in source map)
            if (matches.Count == 0)
            {
                return new SourceLocation(this.EntrypointFilePath, jsonLine + 1); // convert line number back to 1-indexing
            }

            // TODO: verify entrypoint file should always be top of call stack
            if (Path.GetFileName(this.EntrypointFilePath) != matches.Last().filePathRelativeToEntrypoint) throw new Exception();

            SourceLocation sourceLocation = null;
            var entrypointFullPath = Path.GetDirectoryName(this.EntrypointFilePath);
            foreach (var match in matches)
            {
                var matchFullFilePath = Path.GetFullPath(Path.Combine(entrypointFullPath, match.filePathRelativeToEntrypoint));
                sourceLocation = new SourceLocation(matchFullFilePath, match.lineNumber + 1, sourceLocation); // convert line number back to 1-indexing
            }

            return sourceLocation;
        }
    }
}