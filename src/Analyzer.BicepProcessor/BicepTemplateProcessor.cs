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
    /// </summary>
    public class BicepTemplateProcessor
    {
        /// <summary>
        /// Converts Bicep template into JSON template and returns it as a string
        /// </summary>/// <param name="bicepPath">The Bicep Template file path.</param>
        /// <returns>The processed template as a <c>JSON</c> object.</returns>
        static public string ConvertBicepToJson(string bicepPath)
        {
            using var stringWriter = new StringWriter();
            var syntaxTreeGrouping = SyntaxTreeGroupingBuilder.Build(new FileResolver(), new Workspace(), PathHelper.FilePathToFileUrl(bicepPath));
            var compilation = new Compilation(AzResourceTypeProvider.CreateWithAzTypes(), syntaxTreeGrouping);
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), "");
            emitter.Emit(stringWriter);
            return stringWriter.ToString();
        }

    }

}
