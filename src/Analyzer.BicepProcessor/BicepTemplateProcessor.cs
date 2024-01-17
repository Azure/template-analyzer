// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using Bicep.Core;
using Bicep.Core.Analyzers.Interfaces;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Extensions;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.Syntax;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem.Providers;
using Bicep.Core.Utils;
using Bicep.Core.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using BicepEnvironment = Bicep.Core.Utils.Environment;
using IOFileSystem = System.IO.Abstractions.FileSystem;
using SysEnvironment = System.Environment;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
    /// <summary>
    /// Contains functionality to process Bicep templates.
    /// </summary>
    public static class BicepTemplateProcessor
    {
        /// <summary>
        /// DI Helper from Bicep.Cli.Helpers.ServiceCollectionExtensions
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddBicepCore(this IServiceCollection services) => services
            .AddSingleton<INamespaceProvider, DefaultNamespaceProvider>()
            .AddSingleton<IResourceTypeProviderFactory, ResourceTypeProviderFactory>()
            .AddSingleton<IContainerRegistryClientFactory, ContainerRegistryClientFactory>()
            .AddSingleton<ITemplateSpecRepositoryFactory, TemplateSpecRepositoryFactory>()
            .AddSingleton<IModuleDispatcher, ModuleDispatcher>()
            .AddSingleton<IArtifactRegistryProvider, DefaultArtifactRegistryProvider>()
            .AddSingleton<ITokenCredentialFactory, TokenCredentialFactory>()
            .AddSingleton<IFileResolver, FileResolver>()
            .AddSingleton<IEnvironment, BicepEnvironment>()
            .AddSingleton<IFileSystem, IOFileSystem>()
            .AddSingleton<IConfigurationManager, ConfigurationManager>()
            .AddSingleton<IBicepAnalyzer, LinterAnalyzer>()
            .AddSingleton<IFeatureProviderFactory, FeatureProviderFactory>()
            .AddSingleton<FeatureProviderFactory>() // needed for below
            .AddSingleton<IFeatureProviderFactory, SourceMapFeatureProviderFactory>() // enable source mapping
            .AddSingleton<ILinterRulesProvider, LinterRulesProvider>()
            .AddSingleton<BicepCompiler>();

        private static readonly Regex IsModuleRegistryPathRegex = new("^br(:[\\w.]+\\/|\\/public:)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static BicepCompiler BicepCompiler = null;

        /// <summary>
        /// Converts Bicep template into JSON template and returns it as a string and its source map
        /// </summary>
        /// <param name="bicepPath">The Bicep template file path.</param>
        /// <returns>The compiled template as a <c>JSON</c> string and its source map.</returns>
        public static (string, BicepMetadata) ConvertBicepToJson(string bicepPath)
        {
            if (BicepCompiler == null)
            {
                var serviceCollection = new ServiceCollection();
                serviceCollection.AddBicepCore();

                var services = serviceCollection.BuildServiceProvider();
                BicepCompiler = services.GetRequiredService<BicepCompiler>();
            }

            using var stringWriter = new StringWriter();
            var compilation = BicepCompiler.CreateCompilation(new Uri(bicepPath)).Result;
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel());
            var emitResult = emitter.Emit(stringWriter);

            if (emitResult.Status == EmitStatus.Failed)
            {
                var bicepIssues = emitResult.Diagnostics
                    .Where(diag => diag.Level == DiagnosticLevel.Error)
                    .Select(diag => diag.Message);
                throw new Exception($"Bicep issues found:{SysEnvironment.NewLine}{string.Join(SysEnvironment.NewLine, bicepIssues)}");
            }

            string GetPathRelativeToEntryPoint(string absolutePath) => Path.GetRelativePath(
                Path.GetDirectoryName(compilation.SourceFileGrouping.EntryPoint.FileUri.AbsolutePath), absolutePath);

            // Collect all needed module info from SourceFileGrouping metadata
            var moduleInfo = compilation.SourceFileGrouping.FileUriResultByArtifactReference.Select(sourceFileAndMetadata =>
            {
                var bicepSourceFile = sourceFileAndMetadata.Key as BicepSourceFile;
                var pathRelativeToEntryPoint = GetPathRelativeToEntryPoint(bicepSourceFile.FileUri.AbsolutePath);
                var modules = sourceFileAndMetadata.Value
                    .Select(artifactRefAndUriResult =>
                    {
                        // Do not include modules imported from public/private registries, as it is more useful for user to see line number
                        // of the module declaration itself instead of line number in the module as the user does not control template in registry directly
                        if (artifactRefAndUriResult.Key is not ModuleDeclarationSyntax moduleDeclaration
                            || moduleDeclaration.Path is not StringSyntax moduleDeclarationPath
                            || moduleDeclarationPath.SegmentValues.Any(v => IsModuleRegistryPathRegex.IsMatch(v)))
                        {
                            return null;
                        }

                        if (!artifactRefAndUriResult.Value.IsSuccess())
                        {
                            return null;
                        }

                        var moduleLine = TextCoordinateConverter.GetPosition(bicepSourceFile.LineStarts, moduleDeclaration.Span.Position).line;
                        var modulePath = new FileInfo(artifactRefAndUriResult.Value.Unwrap().AbsolutePath).FullName; // converts path to current platform

                        // Use relative paths for bicep to match file paths used in bicep modules and source map
                        if (modulePath.EndsWith(".bicep"))
                        {
                            modulePath = GetPathRelativeToEntryPoint(modulePath);
                        }

                        return new { moduleLine, modulePath };
                    })
                    .WhereNotNull()
                    .ToDictionary(c => c.moduleLine, c => c.modulePath);

                return new SourceFileModuleInfo(pathRelativeToEntryPoint, modules);
            });

            var bicepMetadata = new BicepMetadata()
            {
                ModuleInfo = moduleInfo,
                SourceMap = emitResult.SourceMap
            };

            return (stringWriter.ToString(), bicepMetadata);
        }
    }
}
