// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
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
        public IEnumerable<IEvaluation> EvaluateRules(TemplateContext templateContext, string ruleDefinitions)
        {
            List<RuleDefinition> rules = JsonConvert.DeserializeObject<List<RuleDefinition>>(ruleDefinitions);

            foreach(RuleDefinition rule in rules)
            {
                var ruleExpression = rule.Evaluation.ToExpression();
                var evaluation = ruleExpression.Evaluate(
                    new JsonPathResolver(
                        templateContext.ExpandedTemplate,
                        templateContext.ExpandedTemplate.Path));

                // If there are no matching cases of the rule, do not create an evaluation
                if (!(evaluation as IEvaluation).HasResults())
                {
                    continue;
                }

                evaluation.RuleDefinition = rule;
                evaluation.FileIdentifier = templateContext.TemplateIdentifier;
                evaluation.Results = evaluation.Results.Select(result => PopulateResult(result as JsonRuleResult, templateContext));

                yield return evaluation;
            }
        }

        /// <summary>
        /// Populates additional fields of an evaluation result for added context.
        /// </summary>
        /// <param name="result">The result to populate.</param>
        /// <param name="templateContext">The template that was evaluated.</param>
        /// <returns>The populated result.</returns>
        private JsonRuleResult PopulateResult(JsonRuleResult result, TemplateContext templateContext)
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
            
            result.LineNumber = originalTemplateLineNumber;

            return result;
        }
    }
}
