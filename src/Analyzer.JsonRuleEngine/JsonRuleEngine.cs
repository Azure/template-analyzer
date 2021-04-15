// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
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
        /// <summary>
        /// Delegate for building an <see cref="ILineNumberResolver"/>
        /// </summary>
        /// <param name="context">The <see cref="TemplateContext"/> being evaluated.</param>
        /// <returns>An <see cref="ILineNumberResolver"/> to resolve line numbers for the given template context.</returns>
        public delegate ILineNumberResolver BuildIJsonLineNumberResolver(TemplateContext context);

        private readonly BuildIJsonLineNumberResolver BuildLineNumberResolver;

        /// <summary>
        /// Creates an instance of <see cref="JsonRuleEngine"/>.
        /// </summary>
        /// <param name="jsonLineNumberResolverBuilder">A builder to create an <see cref="ILineNumberResolver"/> for mapping JSON paths from a
        /// processed template to the line number of the equivalent location in the original template.</param>
        public JsonRuleEngine(BuildIJsonLineNumberResolver jsonLineNumberResolverBuilder)
        {
            this.BuildLineNumberResolver = jsonLineNumberResolverBuilder ?? throw new ArgumentNullException(nameof(jsonLineNumberResolverBuilder));
        }

        /// <summary>
        /// Evaluates a template using rules defined in JSON.
        /// </summary>
        /// <param name="templateContext">The template context to evaluate.</param>
        /// <param name="ruleDefinitions">The JSON rules to evaluate the template with.</param>
        /// <returns>The results of the rules against the template.</returns>
        public IEnumerable<IEvaluation> EvaluateRules(TemplateContext templateContext, string ruleDefinitions)
        {
            List<RuleDefinition> rules;

            try
            {
                rules = JsonConvert.DeserializeObject<List<RuleDefinition>>(ruleDefinitions);
            }
            catch (Exception e)
            {
                throw new JsonRuleEngineException($"Failed to parse rules.", e);
            }

            foreach (RuleDefinition rule in rules)
            {
                Expression ruleExpression;

                try
                {
                    ruleExpression = rule.ExpressionDefinition.ToExpression(BuildLineNumberResolver(templateContext));
                }
                catch (Exception e)
                {
                    throw new JsonRuleEngineException($"Failed to parse rule {rule.Name}.", e);
                }

                JsonRuleEvaluation evaluation = ruleExpression.Evaluate(
                    new JsonPathResolver(
                        templateContext.ExpandedTemplate,
                        templateContext.ExpandedTemplate.Path));

                 evaluation.RuleDefinition = rule;
                 evaluation.FileIdentifier = templateContext.TemplateIdentifier;

                // If there are no matching cases of the rule, do not create an evaluation
                if (!evaluation.HasResults)
                {
                    continue;
                }
                    
                yield return evaluation;
            }
        }
    }
}
