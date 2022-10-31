// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Describes the result of a TemplateAnalyzer rule against an ARM or Bicep template.
    /// </summary>
    public class Result : IEquatable<Result>
    {
        /// <summary>
        /// Gets a value indicating whether or not the rule for this result passed.
        /// </summary>
        public bool Passed { get; }

        /// <summary>
        /// Gets the source location where the rule was evaluated.
        /// </summary>
        public SourceLocation SourceLocation { get; }

        /// <summary>
        /// Gets or sets the JSON path to the location in the JSON where the rule was evaluated.
        /// </summary>
        public string JsonPath { get; set; }

        /// <summary>
        /// Gets the expression object associated with this result.
        /// </summary>
        public object Expression { get; set; }

        /// <summary>
        /// Creates a <see cref="Result"/> that represents a result obtained from a rule.
        /// </summary>
        /// <param name="passed">Whether or not the rule for this result passed.</param>
        /// <param name="sourceLocation">The source location where the rule was evaluated.</param>
        /// <param name="jsonPath">The JSON path to the location in the JSON where the rule was evaluated</param>
        /// <param name="expression">The expression associated with this result.</param>
        public Result(bool passed = false, SourceLocation sourceLocation = null, string jsonPath = null, object expression = null)
        {
            Passed = passed;
            SourceLocation = sourceLocation;
            JsonPath = jsonPath;
            Expression = expression;
        }

        /// <inheritdoc/>
        public bool Equals(Result other)
        {
            if (!Passed.Equals(other?.Passed)) return false;

            if (SourceLocation != null)
            {
                if (!SourceLocation.Equals(other.SourceLocation)) return false;
            }
            else if (other.SourceLocation != null) return false;

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            var result = other as Result;
            return (other != null) && Equals(result);
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(this.Passed, this.SourceLocation);
    }
}