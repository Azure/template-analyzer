// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Abstractions;
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

            var featureProvider = new FeatureProvider();
            
            var fileResolver = new FileResolver();
            var clientFactory = new ContainerRegistryClientFactory(new TokenCredentialFactory());
            var templateSpecRepositoryFactory = new TemplateSpecRepositoryFactory(new TokenCredentialFactory());
            var dispatcher = new ModuleDispatcher(new DefaultModuleRegistryProvider(fileResolver, clientFactory, templateSpecRepositoryFactory, featureProvider));
            var configurationManager = new ConfigurationManager(new FileSystem());
            var configuration = configurationManager.GetConfiguration(new Uri(bicepPath));

            var namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader(), featureProvider);
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, dispatcher, new Workspace(), PathHelper.FilePathToFileUrl(bicepPath), configuration);
            var compilation = new Compilation(featureProvider, namespaceProvider, sourceFileGrouping, configuration, new LinterAnalyzer(configuration));

            var settings = new EmitterSettings(featureProvider);
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), settings);
            var emitResult = emitter.Emit(stringWriter);
            
            return (stringWriter.ToString(), emitResult.SourceMap);
        }
    }
}
