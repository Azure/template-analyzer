// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Armory.JsonRuleEngine
{
    /// <summary>
    /// The base class for all Expressions in JSON rules.
    /// </summary>
    internal abstract class Expression
    {
        /// <summary>
        /// Gets the JSON rule this expression is contained in.
        /// </summary>
        protected RuleDefinition Rule { get; private set; }

        /// <summary>
        /// Creates the expression.
        /// </summary>
        /// <param name="rootRule">The rule this expression is contained in.</param>
        public Expression(RuleDefinition rootRule)
        {
            this.Rule = rootRule ?? throw new ArgumentNullException(nameof(rootRule));
        }

        /// <summary>
        /// Executes this <c>Expression</c> against a template.
        /// </summary>
        /// <param name="template">The template to evaluate</param>
        public abstract IEnumerable<JsonRuleResult> Evaluate(IJsonPathResolver jsonScope);
    }
}
