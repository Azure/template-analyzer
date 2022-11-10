// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bicep.Core.Extensions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
    /// <summary>
    /// An <see cref="ISourceLocationResolver"/> used for resolving line numbers from a compiled JSON template to the original Bicep template.
    /// </summary>
    public class BicepSourceLocationResolver : ISourceLocationResolver
    {
        private readonly string EntrypointFilePath;
        private readonly JsonSourceLocationResolver jsonLineNumberResolver;
        private readonly BicepMetadata metadata;
        private readonly TemplateContext templateContext;

        /// <summary>
        /// Create a new instance with the given <see cref="TemplateContext"/>.
        /// </summary>
        /// <param name="templateContext">The template context to map JSON paths against.</param>
        public BicepSourceLocationResolver(TemplateContext templateContext)
        {
            this.EntrypointFilePath = (templateContext ?? throw new ArgumentNullException(nameof(templateContext))).TemplateIdentifier;
            this.templateContext = templateContext;
            this.jsonLineNumberResolver = new(templateContext);
            this.metadata = (templateContext.BicepMetadata as BicepMetadata) ?? throw new ArgumentNullException(nameof(templateContext.BicepMetadata));
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

            // Find the most specific match in source map
            var bestMatch = metadata.SourceMap.Entries
                .Select(sourceFile =>
                {
                    var match = sourceFile.SourceMap.FirstOrDefault(mapping => mapping.TargetLine == jsonLine);
                    var matchSize = match != default
                        ? sourceFile.SourceMap.Count(mapping => mapping.SourceLine == match.SourceLine)
                        : int.MaxValue;
                    return (sourceFile.FilePath, match?.SourceLine, matchSize);
                })
                .MinBy(tuple => tuple.matchSize);

            // default to result from JSON if no matches
            if (!bestMatch.SourceLine.HasValue)
            {
                return new SourceLocation(this.EntrypointFilePath, jsonLine + 1); // convert line number back to 1-indexing
            }

            // check if match is an ARM module reference, if so return location in that template
            var moduleMetadata = this.metadata.ModuleInfo.FirstOrDefault(info => info.FileName == bestMatch.FilePath);
            if (moduleMetadata != default && moduleMetadata.Modules.ContainsKey(bestMatch.SourceLine.Value))
            {
                var modulePath = moduleMetadata.Modules[bestMatch.SourceLine.Value];
                if (modulePath.EndsWith(".json") && File.Exists(modulePath))
                {
                    var template = ArmTemplateCache.GetArmTemplate(modulePath);
                    var token = template.InsensitiveToken(pathInExpandedTemplate, InsensitivePathNotFoundBehavior.LastValid);
                    var lineNumber = (token as IJsonLineInfo)?.LineNumber;

                    // fall back to location of module refernce in parent file if failing to get line number
                    if (lineNumber != null)
                    {
                        return new SourceLocation(modulePath, lineNumber.Value);
                    }
                }
            }

            var entrypointFullPath = Path.GetDirectoryName(this.EntrypointFilePath);
            var matchFullFilePath = Path.GetFullPath(Path.Combine(entrypointFullPath, bestMatch.FilePath));
            return new SourceLocation(matchFullFilePath, bestMatch.SourceLine.Value + 1); // convert line number back to 1-indexing
        }

        private static class ArmTemplateCache
        {
            private static readonly Dictionary<string, JObject> Templates = new();

            public static JObject GetArmTemplate(string templatePath)
            {
                if (!Templates.ContainsKey(templatePath))
                {
                    // assumption: template has already been previously parsed by Bicep library and was valid
                    Templates[templatePath] = JObject.Parse(File.ReadAllText(templatePath));
                }

                return Templates[templatePath];
            }
        }
    }
}