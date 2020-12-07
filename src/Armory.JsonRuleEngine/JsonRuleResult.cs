// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Armory.Types;

namespace Armory.JsonRuleEngine
{
    /// <summary>
    /// Describes the result of an ARMory JSON rule against an ARM template.
    /// </summary>
    internal class JsonRuleResult : IResult
    {
        /// <summary>
        /// Gets or sets the JSON rule this result is for.
        /// </summary>
        internal RuleDefinition RuleDefinition { get; set; }

        /// <summary>
        /// Gets the name of the rule this result is for.
        /// </summary>
        public string RuleName => RuleDefinition.Name;

        /// <summary>
        /// Gets the description of the rule this result is for.
        /// </summary>
        public string RuleDescription => RuleDefinition.Description;

        /// <summary>
        /// Gets the recommendation for addressing this result.
        /// </summary>
        public string Recommendation => RuleDefinition.Recommendation;

        /// <summary>
        /// Gets the Uri where help for this result can be found.
        /// </summary>
        public string HelpUri => RuleDefinition.HelpUri;

        /// <summary>
        /// Gets a value indicating whether or not the rule for this result passed.
        /// </summary>
        public bool Passed { get; internal set; }

        /// <summary>
        /// Gets the identifier of the file this result is for.
        /// </summary>
        public string FileIdentifier => throw new System.NotImplementedException();

        /// <summary>
        /// Gets the line number of the file where the rule was evaluated.
        /// </summary>
        public int LineNumber => throw new System.NotImplementedException();
    }
}
