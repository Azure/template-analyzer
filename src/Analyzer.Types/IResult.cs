// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

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
        /// Gets the list of file names and line numbers where the rule was evaluated TODO: module
        /// </summary>
        public SourceLocation SourceLocation { get; }
    }
}
