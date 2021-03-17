// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Describes the result of a TemplateAnalyzer rule against an ARM template.
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Gets a value indicating whether or not the rule for this result passed.
        /// </summary>
        public bool Passed { get; }

        /// <summary>
        /// Gets the line number of the file where the rule was evaluated.
        /// </summary>
        public int LineNumber { get; }
    }
}
