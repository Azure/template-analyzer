// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
{
    /// <summary>
    /// Describes the result of a TemplateAnalyzer JSON rule against an ARM template.
    /// </summary>
    internal class JsonRuleResult : IResult
    {
        /// <summary>
        /// Gets a value indicating whether or not the rule for this result passed.
        /// </summary>
        public bool Passed { get; internal set; }

        /// <summary>
        /// Gets the line number of the file where the rule was evaluated.
        /// </summary>
        public int LineNumber { get; internal set; }

        /// <summary>
        /// Gets or sets the JSON path to the location in the JSON where the rule was evaluated.
        /// </summary>
        internal string JsonPath { get; set; }

        /// <summary>
        /// Gets the expression associated with this result
        /// </summary>
        internal Expression Expression { get; set; }
    }
}
