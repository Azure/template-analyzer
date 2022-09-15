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
        /// Gets the source location where the rule was evaluated.
        /// </summary>
        public SourceLocation SourceLocation { get; }
    }
}
