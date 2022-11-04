using Bicep.Core.Features;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
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
        public bool RegistryEnabled => features.RegistryEnabled;

        /// <inheritdoc/>
        public bool SymbolicNameCodegenEnabled => features.SymbolicNameCodegenEnabled;

        /// <inheritdoc/>
        public bool ExtensibilityEnabled => features.ExtensibilityEnabled;

        /// <inheritdoc/>
        public bool ResourceTypedParamsAndOutputsEnabled => features.ResourceTypedParamsAndOutputsEnabled;

        /// <inheritdoc/>
        public bool SourceMappingEnabled => true;

        /// <inheritdoc/>
        public bool ParamsFilesEnabled => features.ParamsFilesEnabled;

        /// <inheritdoc/>
        public bool UserDefinedTypesEnabled => features.UserDefinedTypesEnabled;
    }
}
