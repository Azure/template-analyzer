// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Analyzers.Linter.ApiVersions;
using Bicep.Core.Configuration;
using Bicep.Core.Diagnostics;
using Bicep.Core.Emit;
using Bicep.Core.Features;
using Bicep.Core.FileSystem;
using Bicep.Core.Registry;
using Bicep.Core.Registry.Auth;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.Text;
using Bicep.Core.TypeSystem.Az;
using Bicep.Core.Workspaces;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
    /// <summary>
    /// Helper class containing metadata from Bicep compilation necessary for later analysis
    /// </summary>
    public class BicepMetadata
    {
        /// <summary>
        /// Contains metadata for modules in source file
        /// </summary>
        public IEnumerable<SourceFileModuleInfo> ModuleInfo;

        /// <summary>
        /// Maps line numbers from resultant compiled ARM template back to original Bicep source files
        /// </summary>
        public SourceMap SourceMap;
    }

    /// <summary>
    /// Helper class to contain information for all modules in each source file
    /// </summary>
    public class SourceFileModuleInfo : IEquatable<SourceFileModuleInfo>
    {
        /// <summary>
        /// File path of source file
        /// </summary>
        public string FileName;

        /// <summary>
        /// Dictionary mapping line numbers containing module references in source file to file path of referenced modules
        /// </summary>
        public Dictionary<int, string> Modules;

        /// <summary>
        /// Create instance of SourceFileModuleInfo
        /// </summary>
        /// <param name="fileName">File path of source file</param>
        /// <param name="modules">Dictionary of modules in source file</param>
        public SourceFileModuleInfo(string fileName, Dictionary<int, string> modules)
        {
            FileName = fileName;
            Modules = modules;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var result = obj as SourceFileModuleInfo;
            return (result != null) && Equals(result);
        }

        /// <inheritdoc/>
        public bool Equals(SourceFileModuleInfo moduleInfo)
        {
            return this.FileName.Equals(moduleInfo.FileName)
                && this.Modules.Equals(moduleInfo.Modules);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.FileName, this.Modules);
    }

    /// <summary>
    /// Contains functionality to process Bicep templates.
    /// </summary>
    public class BicepTemplateProcessor
    {
        private static readonly IConfigurationManager configurationManager = new ConfigurationManager(new FileSystem());
        private static readonly IFileResolver fileResolver = new FileResolver(new FileSystem());
        private static readonly INamespaceProvider namespaceProvider = new DefaultNamespaceProvider(new AzResourceTypeLoader());

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
                    new ContainerRegistryClientFactory(new TokenCredentialFactory()),
                    new TemplateSpecRepositoryFactory(new TokenCredentialFactory()),
                    featureProviderFactory,
                    configurationManager),
                configurationManager);
            var workspace = new Workspace();
            var sourceFileGrouping = SourceFileGroupingBuilder.Build(fileResolver, moduleDispatcher, workspace, PathHelper.FilePathToFileUrl(bicepPath));

            // pull modules optimistically
            if (moduleDispatcher.RestoreModules(moduleDispatcher.GetValidModuleReferences(sourceFileGrouping.GetModulesToRestore())).Result)
            {
                // modules had to be restored - recompile
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

            // collect all needed module info from sourceFileGrouping metadata
            var moduleInfo = sourceFileGrouping.UriResultByModule.Select(kvp =>
            {
                var bicepSourceFile = kvp.Key as BicepSourceFile;
                var pathRelativeToEntryPoint = GetPathRelativeToEntryPoint(bicepSourceFile.FileUri.AbsolutePath);
                var modules = kvp.Value.Values
                    .Select(result =>
                    {
                        var moduleLine = TextCoordinateConverter.GetPosition(bicepSourceFile.LineStarts, result.Statement.Span.Position).line;
                        var modulePath = result.FileUri.AbsolutePath;

                        // use relative paths for bicep to match file paths used in bicep modules and source map
                        if (modulePath.EndsWith(".bicep"))
                        {
                            modulePath = GetPathRelativeToEntryPoint(modulePath);
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
