// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Armory.JsonRuleEngine
{
    internal abstract class Expression
    {
        protected RuleDefinition Rule { get; private set; }

        public Expression(RuleDefinition rootRule)
        {
            this.Rule = rootRule ?? throw new ArgumentNullException(nameof(rootRule));
        }

        /// <summary>
        /// Executes this <c>Expression</c> against a template.
        /// TODO: This method will evolve as more of ARMory is implemented.
        /// </summary>
        /// <param name="template">The template to evaluate</param>
        public abstract IEnumerable<JsonRuleResult> Evaluate(IJsonPathResolver template);
    }
}
