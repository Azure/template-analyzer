// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Extensions;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.Syntax;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
    /// <summary>
    /// Contains functionality to process Bicep templates.
    /// </summary>
    public class BicepTemplateProcessor
    {
        private static readonly Regex IsModuleRegistryPathRegex = new("^br(:[\\w.]+\\/|\\/public:)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly IConfigurationManager configurationManager = new ConfigurationManager(new FileSystem());
        private static readonly IFileResolver fileResolver = new FileResolver(new FileSystem());
        private static readonly INamespaceProvider namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader());
        private static readonly ITokenCredentialFactory tokenCredentialFactory = new TokenCredentialFactory();

        /// <summary>
        /// Converts Bicep template into JSON template and returns it as a string and its source map
        /// </summary>
        /// <param name="bicepPath">The Bicep template file path.</param>
        /// <returns>The compiled template as a <c>JSON</c> string and its source map.</returns>
        public static (string, BicepMetadata) ConvertBicepToJson(string bicepPath)
        {
            var bicepPathUri = new Uri(bicepPath);
            using var stringWriter = new StringWriter();

            var configuration = configurationManager.GetConfiguration(bicepPathUri);

            var featureProviderFactory = IFeatureProviderFactory.WithStaticFeatureProvider(
                new SourceMapFeatureProvider(new FeatureProvider(configuration)));
            var apiVersionProvider = new ApiVersionProvider(
                featureProviderFactory.GetFeatureProvider(bicepPathUri), namespaceProvider);
            var apiVersionProviderFactory = IApiVersionProviderFactory.WithStaticApiVersionProvider(apiVersionProvider);

            var moduleDispatcher = new ModuleDispatcher(
                new DefaultModuleRegistryProvider(
                    fileResolver,
                    new ContainerRegistryClientFactory(tokenCredentialFactory),
                    new TemplateSpecRepositoryFactory(tokenCredentialFactory),
                    featureProviderFactory,
                    configurationManager),
                configurationManager);
            var workspace = new Workspace();
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, PathHelper.FilePathToFileUrl(bicepPath));

            // Pull modules optimistically
            if (moduleDispatcher.RestoreModules(moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.GetModulesToRestore())).Result)
            {
                // Modules had to be restored - recompile
                sourceFileGrouping = SourceFileGroupingBuilder.Rebuild(moduleDispatcher, workspace, sourceFileGrouping);
            }

            var compilation = new Compilation(featureProviderFactory, namespaceProvider, sourceFileGrouping, configurationManager, apiVersionProviderFactory, new LinterAnalyzer());
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel());
            var emitResult = emitter.Emit(stringWriter);

            if (emitResult.Status == EmitStatus.Failed)
            {
                var bicepIssues = emitResult.Diagnostics
                    .Where(diag => diag.Level == DiagnosticLevel.Error)
                    .Select(diag => diag.Message);
                throw new Exception($"Bicep issues found:{Environment.NewLine}{string.Join(Environment.NewLine, bicepIssues)}");
            }

            string GetPathRelativeToEntryPoint(string absolutePath) => Path.GetRelativePath(
                Path.GetDirectoryName(sourceFileGrouping.EntryPoint.FileUri.AbsolutePath), absolutePath);

            // Collect all needed module info from sourceFileGrouping metadata
            var moduleInfo = sourceFileGrouping.UriResultByModule.Select(kvp =>
            {
                var bicepSourceFile = kvp.Key as BicepSourceFile;
                var pathRelativeToEntryPoint = GetPathRelativeToEntryPoint(bicepSourceFile.FileUri.AbsolutePath);
                var modules = kvp.Value.Values
                    .Select(result =>
                    {
                        // Do not include modules imported from public/private registries, as it is more useful for user to see line number
                        // of the module declaration itself instead of line number in the module as the user does not control template in registry directly
                        if (result.Statement is not ModuleDeclarationSyntax moduleDeclaration
                            || moduleDeclaration.Path is not StringSyntax moduleDeclarationPath
                            || moduleDeclarationPath.SegmentValues.Any(v => IsModuleRegistryPathRegex.IsMatch(v)))
                        {
                            return null;
                        }

                        var moduleLine = TextCoordinateConverter.GetPosition(bicepSourceFile.LineStarts, moduleDeclaration.Span.Position).line;
                        var modulePath = new FileInfo(result.FileUri.AbsolutePath).FullName; // converts path to current platform

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
