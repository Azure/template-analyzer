// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
using Bicep.Core.Navigation;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Registry.PublicRegistry;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.Syntax;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem.Providers;
using Bicep.Core.Utils;
using Bicep.Core.Workspaces;
using Bicep.IO.Abstraction;
using Bicep.IO.FileSystem;
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
            .AddSingleton<INamespaceProvider, NamespaceProvider>()
            .AddSingleton<IResourceTypeProviderFactory, ResourceTypeProviderFactory>()
            .AddSingleton<IContainerRegistryClientFactory, ContainerRegistryClientFactory>()
            .AddSingleton<IPublicRegistryModuleMetadataProvider, PublicRegistryModuleMetadataProvider>()
            .AddSingleton<ITemplateSpecRepositoryFactory, TemplateSpecRepositoryFactory>()
            .AddSingleton<IModuleDispatcher, ModuleDispatcher>()
            .AddSingleton<IArtifactRegistryProvider, DefaultArtifactRegistryProvider>()
            .AddSingleton<ITokenCredentialFactory, TokenCredentialFactory>()
            .AddSingleton<IFileResolver, FileResolver>()
            .AddSingleton<IFileExplorer, FileSystemFileExplorer>()
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

            string entryPointDirectory = Path.GetDirectoryName(compilation.SourceFileGrouping.EntryPoint.Uri.AbsolutePath);

            bool IsResolvedLocalModuleReference(KeyValuePair<IArtifactReferenceSyntax, ArtifactResolutionInfo> artifact) =>
                // Only include local module references (not modules imported from public/private registries, i.e. those that match IsModuleRegistryPathRegex),
                // as it is more useful for user to see line number of the module declaration itself,
                // rather than the line number in the module (as the user does not control the template in the registry directly).
                artifact.Key is ModuleDeclarationSyntax moduleDeclaration &&
                moduleDeclaration.Path is StringSyntax moduleDeclarationPath &&
                !moduleDeclarationPath.SegmentValues.Any(IsModuleRegistryPathRegex.IsMatch) &&
                artifact.Value.Result.IsSuccess();

            // Create SourceFileModuleInfo collection by gathering all needed module info from SourceFileGrouping metadata.
            // Group by the source file path to allow for easy construction of SourceFileModuleInfo.
            var moduleInfo = compilation.SourceFileGrouping.ArtifactLookup
                .Where(IsResolvedLocalModuleReference)
                .GroupBy(artifact => artifact.Value.Origin)
                .Select(grouping =>
                {
                    var bicepSourceFile = grouping.Key;
                    var pathRelativeToEntryPoint = Path.GetRelativePath(
                        Path.GetDirectoryName(compilation.SourceFileGrouping.EntryPoint.Uri.AbsolutePath), bicepSourceFile.Uri.AbsolutePath);

                    // Use the grouping value (KeyValuePair<IArtifactReferenceSyntax,ArtifactResolutionInfo>) to create
                    // a dictionary of module line numbers to file paths.
                    // This represents the modules in the source file, and where (what lines) they are referenced.
                    var modules = grouping.Select(artifactRefAndUriResult =>
                    {
                        var module = artifactRefAndUriResult.Key as ModuleDeclarationSyntax;
                        var moduleLine = TextCoordinateConverter.GetPosition(bicepSourceFile.LineStarts, module.Span.Position).line;
                        var modulePath = new FileInfo(artifactRefAndUriResult.Value.Result.Unwrap().AbsolutePath).FullName; // converts path to current platform

                        // Use relative paths for bicep to match file paths used in bicep modules and source map
                        if (modulePath.EndsWith(".bicep"))
                        {
                            modulePath = Path.GetRelativePath(entryPointDirectory, modulePath);
                        }

                        return new { moduleLine, modulePath };
                    })
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
