// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using Bicep.Core.Emit;
using Bicep.Core.FileSystem;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
    /// <summary>
    /// Contains functionality to process Bicep templates.
    /// Contains functionality to convert Bicep template into JSON templates.
    /// </summary>
    public class BicepTemplateProcessor
    {
        private readonly string bicepPath;

        /// <summary>
        ///  Constructor for the Bicep Template Processing library
        /// </summary>
        /// <param name="bicepPath">The Bicep Template file path. Needed to convert to JSON.</param>
        public BicepTemplateProcessor(string bicepPath)
        {
            this.bicepPath = bicepPath;
        }

        /// <summary>
        /// Converts Bicep template into JSON template and returns it as a string
        /// </summary>
        /// <returns>The processed template as a <c>JSON</c> object.</returns>
        public string ConvertBicepToJson()
        {
            using var stringWriter = new StringWriter();
            var syntaxTreeGrouping = SyntaxTreeGroupingBuilder.Build(new FileResolver(), new Workspace(), PathHelper.FilePathToFileUrl(bicepPath));
            var compilation = new Compilation(AzResourceTypeProvider.CreateWithAzTypes(), syntaxTreeGrouping);
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), "0.4.123.62267");
            emitter.Emit(stringWriter);
            return stringWriter.ToString();
        }

    }

}
