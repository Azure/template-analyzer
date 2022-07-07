// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Configuration;
using Bicep.Core.Emit;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
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
        /// </summary>
        /// <param name="bicepPath">The Bicep Template file path.</param>
        /// <returns>The compiled template as a <c>JSON</c> string and its source map.</returns>
        static public (string, SourceMap) ConvertBicepToJson(string bicepPath)
        {
            using var stringWriter = new StringWriter();

            Environment.SetEnvironmentVariable("BICEP_SOURCEMAPPING_ENABLED", "true");

            var fileResolver = new FileResolver();
            var featureProvider = new FeatureProvider();
            
            var moduleDispatcher = new ModuleDispatcher(
                new DefaultModuleRegistryProvider(
                    fileResolver,
                    new ContainerRegistryClientFactory(new TokenCredentialFactory()),
                    new TemplateSpecRepositoryFactory(new TokenCredentialFactory()),
                    featureProvider));
            var configuration = (new ConfigurationManager(new FileSystem()).GetConfiguration(new Uri(bicepPath)));

            var compilation = new Compilation(
                featureProvider,
                new DefaultNamespaceProvider( new AzResourceTypeLoader(), featureProvider),
                SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, new Workspace(), PathHelper.FilePathToFileUrl(bicepPath), configuration),
                configuration,
                new LinterAnalyzer(configuration));

            var settings = new EmitterSettings(featureProvider);
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), settings);
            var emitResult = emitter.Emit(stringWriter);

            if (emitResult.Status == EmitStatus.Failed)
            {
                var bicepDiags = emitResult.Diagnostics.Select(diag => diag.Message);
                var bicepIssues = string.Join('\n', bicepDiags);
                throw new Exception($"Bicep issues found:\n{bicepIssues}");
            }
            
            return (stringWriter.ToString(), emitResult.SourceMap);
        }
    }
}
