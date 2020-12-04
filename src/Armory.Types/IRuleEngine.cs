// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Armory.Types
{
    public interface IRuleEngine
    {
        /// <summary>
        /// Evaluates a template using the specified rules.
        /// </summary>
        /// <param name="templateContext">The template context to evaluate.</param>
        /// <param name="ruleDefinitions">The rules to evaluate the template with.</param>
        /// <returns>The results of the rules against the template.</returns>
        public IEnumerable<IResult> Run(TemplateContext templateContext, string ruleDefinitions);
    }
}
