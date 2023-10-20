using System;
using Azure.ResourceManager.Resources;
using Bicep.Core.Features;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
    /// <summary>
    /// Helper class that enables source mapping feature in Bicep.Core
    /// </summary>
    public class SourceMapFeatureProviderFactory : IFeatureProviderFactory
    {
        private readonly IFeatureProviderFactory factory;

        /// <inheritdoc/>
        public SourceMapFeatureProviderFactory(FeatureProviderFactory factory)
        {
            this.factory = factory;
        }

        /// <inheritdoc/>
        public IFeatureProvider GetFeatureProvider(Uri templateUri)
            => new SourceMapFeatureProvider(this.factory.GetFeatureProvider(templateUri));
    }

    /// <summary>
    /// Helper class that enables source mapping feature in Bicep.Core
    /// </summary>
    public class SourceMapFeatureProvider : IFeatureProvider
    {
        private readonly IFeatureProvider features;

        /// <inheritdoc/>
        public SourceMapFeatureProvider(IFeatureProvider features)
        {
            this.features = features;
        }

        /// <inheritdoc/>
        public string AssemblyVersion => features.AssemblyVersion;

        /// <inheritdoc/>
        public string CacheRootDirectory => features.CacheRootDirectory;

        /// <inheritdoc/>
        public bool SymbolicNameCodegenEnabled => features.SymbolicNameCodegenEnabled;

        /// <inheritdoc/>
        public bool ExtensibilityEnabled => features.ExtensibilityEnabled;

        /// <inheritdoc/>
        public bool ResourceTypedParamsAndOutputsEnabled => features.ResourceTypedParamsAndOutputsEnabled;

        /// <inheritdoc/>
        public bool SourceMappingEnabled => true;

        /// <inheritdoc/>
        public bool UserDefinedFunctionsEnabled => features.UserDefinedFunctionsEnabled;

        /// <inheritdoc/>
        public bool DynamicTypeLoadingEnabled => features.DynamicTypeLoadingEnabled;

        /// <inheritdoc/>
        public bool PrettyPrintingEnabled => features.PrettyPrintingEnabled;

        /// <inheritdoc/>
        public bool TestFrameworkEnabled => features.TestFrameworkEnabled;

        /// <inheritdoc/>
        public bool AssertsEnabled => features.AssertsEnabled;

        /// <inheritdoc/>
        public bool CompileTimeImportsEnabled => features.CompileTimeImportsEnabled;

        /// <inheritdoc/>
        public bool MicrosoftGraphPreviewEnabled => features.MicrosoftGraphPreviewEnabled;

        /// <inheritdoc/>
        public bool PublishSourceEnabled => features.PublishSourceEnabled;
    }
}
