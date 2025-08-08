using System;
using Azure.ResourceManager.Resources;
using Bicep.Core.Features;
using Bicep.IO.Abstraction;

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
        public IDirectoryHandle CacheRootDirectory => features.CacheRootDirectory;

        /// <inheritdoc/>
        public bool SymbolicNameCodegenEnabled => features.SymbolicNameCodegenEnabled;

        /// <inheritdoc/>
        public bool ResourceTypedParamsAndOutputsEnabled => features.ResourceTypedParamsAndOutputsEnabled;

        /// <inheritdoc/>
        public bool SourceMappingEnabled => true;

        /// <inheritdoc/>
        public bool TestFrameworkEnabled => features.TestFrameworkEnabled;

        /// <inheritdoc/>
        public bool AssertsEnabled => features.AssertsEnabled;

        /// <inheritdoc/>
        public bool LegacyFormatterEnabled => features.LegacyFormatterEnabled;

        /// <inheritdoc/>
        public bool LocalDeployEnabled => features.LocalDeployEnabled;

        /// <inheritdoc/>
        public bool ExtendableParamFilesEnabled => features.ExtendableParamFilesEnabled;

        /// <inheritdoc/>
        public bool ResourceInfoCodegenEnabled => features.ResourceInfoCodegenEnabled;

        /// <inheritdoc/>
        public bool WaitAndRetryEnabled => features.WaitAndRetryEnabled;

        /// <inheritdoc/>
        public bool OnlyIfNotExistsEnabled => features.OnlyIfNotExistsEnabled;

        /// <inheritdoc/>
        public bool ModuleExtensionConfigsEnabled => features.ModuleExtensionConfigsEnabled;

        /// <inheritdoc/>
        public bool DesiredStateConfigurationEnabled => features.DesiredStateConfigurationEnabled;

        /// <inheritdoc/>
        public bool ModuleIdentityEnabled => features.ModuleIdentityEnabled;
    }
}
