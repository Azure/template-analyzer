// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
{
    /// <summary>
    /// Describes the result of a TemplateAnalyzer JSON rule against an ARM template.
    /// </summary>
    internal class JsonRuleResult : IResult, IEquatable<JsonRuleResult>
    {
        /// <summary>
        /// Gets a value indicating whether or not the rule for this result passed.
        /// </summary>
        public bool Passed { get; internal set; }

        /// <summary>
        /// Gets the source location where the rule was evaluated.
        /// </summary>
        public SourceLocation SourceLocation { get; internal set; }

        /// <summary>
        /// Gets or sets the JSON path to the location in the JSON where the rule was evaluated.
        /// </summary>
        internal string JsonPath { get; set; }

        /// <summary>
        /// Gets the expression associated with this result
        /// </summary>
        internal Expression Expression { get; set; }

        public bool Equals(JsonRuleResult other)
        {
            return Passed.Equals(other?.Passed)
                && SourceLocation.Equals(other.SourceLocation);
        }

        public override bool Equals(object other)
        {
            var result = other as JsonRuleResult;
            return (other != null) && Equals(result);
        }

        public override int GetHashCode()
        {
            return Passed.GetHashCode() ^ SourceLocation.GetHashCode();
        }
    }
}
