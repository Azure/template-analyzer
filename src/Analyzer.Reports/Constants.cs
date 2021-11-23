// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// Definitions of static constant variables
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Tool name to be displayed in the report.
        /// </summary>
        public const string ToolName = "ARM BPA";

        /// <summary>
        /// Full name of tool to be displayed in the report.
        /// </summary>
        public const string ToolFullName = "ARM Template Best Practice Analyzer";

        /// <summary>
        /// Tool version to be displayed in the report.
        /// </summary>
        public static string ToolVersion =
            ((AssemblyInformationalVersionAttribute)
                Assembly.GetExecutingAssembly()
                .GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute))
            ).InformationalVersion;

        /// <summary>
        /// Organization name to be displayed in the report.
        /// </summary>
        public const string Organization = "Microsoft";

        /// <summary>
        /// Tool's information Uri to be displayed in the report.
        /// </summary>
        public const string InformationUri = "https://github.com/Azure/template-analyzer";
    }
}
