// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        public const string ToolVersion = "0.0.2-alpha"; // Should use dynamic version string. issue #197

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
