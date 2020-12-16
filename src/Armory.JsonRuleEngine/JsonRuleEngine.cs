// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Armory.JsonRuleEngine;
using Armory.Types;
using Newtonsoft.Json;

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
        public IEnumerable<IResult> EvaluateRules(TemplateContext templateContext, string ruleDefinitions)
        {
            List<RuleDefinition> rules = JsonConvert.DeserializeObject<List<RuleDefinition>>(ruleDefinitions);
            List<IResult> results = new List<IResult>();

            foreach(RuleDefinition rule in rules)
            {
                results.AddRange(rule.Evaluation.ToExpression(rule).Evaluate(new JsonPathResolver(templateContext.ExpandedTemplate, templateContext.ExpandedTemplate.Path)));
            }

            return results;
        }
    }
}
