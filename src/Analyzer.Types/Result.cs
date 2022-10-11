// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Describes the result of a TemplateAnalyzer rule against an ARM or Bicep template.
    /// </summary>
    public abstract class Result : IEquatable<Result>
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
        /// Creates a <see cref="Result"/> that represents a result obtained from a rule.
        /// </summary>
        /// <param name="passed">Whether or not the rule for this result passed.</param>
        /// <param name="sourceLocation">The source location where the rule was evaluated.</param>
        public Result(bool passed, SourceLocation sourceLocation)
        {
            Passed = passed;
            SourceLocation = sourceLocation;
        }

        /// <inheritdoc/>
        public bool Equals(Result other)
        {
            return GetType().Equals(other.GetType())
                && Passed.Equals(other?.Passed)
                && SourceLocation.Equals(other.SourceLocation);
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            var result = other as Result;
            return (other != null) && Equals(result);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Passed.GetHashCode() ^ SourceLocation.GetHashCode();
        }
    }
}