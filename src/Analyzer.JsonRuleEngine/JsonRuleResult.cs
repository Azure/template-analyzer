// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
{
    /// <summary>
    /// Describes the result of a TemplateAnalyzer JSON rule against an ARM or Bicep template.
    /// </summary>
    internal class JsonRuleResult : Result
    {
        public JsonRuleResult(bool passed = false, SourceLocation sourceLocation = null, string jsonPath = null, Expression expression = null) : base(passed, sourceLocation)
        {
            JsonPath = jsonPath;
            Expression = expression;
        }

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
