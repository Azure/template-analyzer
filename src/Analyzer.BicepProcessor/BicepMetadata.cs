// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Bicep.Core.Emit;

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
}
