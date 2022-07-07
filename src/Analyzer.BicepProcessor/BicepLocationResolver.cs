using System;
using System.Linq;
using Bicep.Core.Emit;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An <see cref="ILocationResolver"/> used for resolving line numbers from an expanded JSON template to the original JSON template.
    /// </summary>
    public class BicepLocationResolver : ILocationResolver
    {
        private readonly JsonLineNumberResolver jsonLineNumberResolver;
        private readonly SourceMap sourceMap;

        /// <summary>
        /// Create a new instance with the given <see cref="TemplateContext"/>.
        /// </summary>
        /// <param name="templateContext">The template context to map JSON paths against.</param>
        public BicepLocationResolver(TemplateContext templateContext)
        {
            this.jsonLineNumberResolver = new(templateContext ?? throw new ArgumentNullException(nameof(templateContext)));
            this.sourceMap = templateContext.SourceMap ?? throw new ArgumentNullException(nameof(templateContext.SourceMap));
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
            var jsonLine = this.jsonLineNumberResolver.ResolveLineNumber(pathInExpandedTemplate);

            var entrypoint = this.sourceMap.Entrypoint;
            var fileSourceMap = this.sourceMap.Entries.First(); // TODO: check entrypoint from source..? or just search for line number

            var bicepLocation = fileSourceMap.SourceMap.Where(mapping => mapping.TargetLine == jsonLine).FirstOrDefault();
            var bicepLine = bicepLocation.SourceLine;

            return bicepLine;
        }
    }
}