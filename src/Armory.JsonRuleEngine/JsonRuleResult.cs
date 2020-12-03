// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Armory.Types;

namespace Armory.JsonRuleEngine
{
    internal class JsonRuleResult : IResult
    {
        /// <summary>
        /// Gets or sets the JSON rule this result is for
        /// </summary>
        internal RuleDefinition RuleDefinition { get; set; }

        public string RuleName => RuleDefinition.Name;

        public string RuleDescription => RuleDefinition.Description;

        public string Recommendation => RuleDefinition.Recommendation;

        public string HelpUri => RuleDefinition.HelpUri;

        public bool Passed { get; internal set; }

        public string FileIdentifier => throw new System.NotImplementedException();

        public int LineNumber => throw new System.NotImplementedException();
    }
}
