// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Armory.Types
{
    /// <summary>
    /// Describes the result of an ARMory rule against an ARM template
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Gets the name of the rule this result is for
        /// </summary>
        public string RuleName { get; }

        /// <summary>
        /// Gets the description of the rule this result is for
        /// </summary>
        public string RuleDescription { get; }

        /// <summary>
        /// Gets the recommendation for addressing this result
        /// </summary>
        public string Recommendation { get; }

        /// <summary>
        /// Gets the Uri where help for this result can be found
        /// </summary>
        public string HelpUri { get; }

        /// <summary>
        /// Gets a value indicating whether or not the rule for this result passed
        /// </summary>
        public bool Passed { get; }

        /// <summary>
        /// Gets the identifier of the file this result is for
        /// </summary>
        public string FileIdentifier { get; }

        /// <summary>
        /// Gets the line number of the file where the rule was evaluated
        /// </summary>
        public int LineNumber { get; }
    }
}
