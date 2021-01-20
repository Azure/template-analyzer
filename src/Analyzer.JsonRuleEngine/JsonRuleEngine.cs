// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.JsonRuleEngine;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.JsonEngine
{
    /// <summary>
    /// Evaluation engine for rules authored in JSON
    /// </summary>
    public class JsonRuleEngine : IRuleEngine
    {
        private readonly IJsonLineNumberResolver lineNumberResolver;

        /// <summary>
        /// Creates an instance of <c>JsonRuleEngine</c>.
        /// </summary>
        /// <param name="lineNumberResolver">An <c>ILineNumberResolver</c> for mapping JSON paths from a
        /// processed template to the line number of the equivalent location in the original template.</param>
        public JsonRuleEngine(IJsonLineNumberResolver lineNumberResolver)
        {
            this.lineNumberResolver = lineNumberResolver ?? throw new ArgumentNullException(nameof(lineNumberResolver));
        }

        /// <summary>
        /// Evaluates a template using rules defined in JSON.
        /// </summary>
        /// <param name="templateContext">The template context to evaluate.</param>
        /// <param name="ruleDefinitions">The JSON rules to evaluate the template with.</param>
        /// <returns>The results of the rules against the template.</returns>
        public IEnumerable<IResult> EvaluateRules(TemplateContext templateContext, string ruleDefinitions)
        {
            List<RuleDefinition> rules = JsonConvert.DeserializeObject<List<RuleDefinition>>(ruleDefinitions);

            foreach(RuleDefinition rule in rules)
            {
                var ruleExpression = rule.Evaluation.ToExpression();
                var ruleResults = ruleExpression.Evaluate(
                    new JsonPathResolver(
                        templateContext.ExpandedTemplate,
                        templateContext.ExpandedTemplate.Path));

                foreach (var result in ruleResults)
                {
                    yield return PopulateResult(result, rule, templateContext);
                }
            }
        }

        /// <summary>
        /// Populates additional fields of an evaluation result for added context.
        /// </summary>
        /// <param name="result">The result to populate.</param>
        /// <param name="rule">The rule the results are for.</param>
        /// <param name="templateContext">The template that was evaluated.</param>
        /// <returns>The populated result.</returns>
        private JsonRuleResult PopulateResult(JsonRuleResult result, RuleDefinition rule, TemplateContext templateContext)
        {
            int originalTemplateLineNumber = 0;

            try
            {
                originalTemplateLineNumber = this.lineNumberResolver.ResolveLineNumberForOriginalTemplate(
                    result.JsonPath,
                    templateContext.ExpandedTemplate,
                    templateContext.OriginalTemplate);
            }
            catch (Exception) { }

            result.RuleDefinition = rule;
            result.FileIdentifier = templateContext.TemplateIdentifier;
            result.LineNumber = originalTemplateLineNumber;

            return result;
        }
    }
}
