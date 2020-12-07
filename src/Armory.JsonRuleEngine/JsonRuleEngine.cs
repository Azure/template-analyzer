// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Armory.Types;
using System.Collections.Generic;

namespace Armory.JsonEngine
{
    /// <summary>
    /// Evaluation engine for rules authored in JSON
    /// </summary>
    public class JsonRuleEngine : IRuleEngine
    {
        /// <summary>
        /// Evaluates a template using rules defined in JSON.
        /// </summary>
        /// <param name="templateContext">The template context to evaluate.</param>
        /// <param name="ruleDefinitions">The JSON rules to evaluate the template with.</param>
        /// <returns>The results of the rules against the template.</returns>
        public IEnumerable<IResult> Run(TemplateContext templateContext, string ruleDefinitions)
        {
            throw new System.NotImplementedException();
        }
    }
}
