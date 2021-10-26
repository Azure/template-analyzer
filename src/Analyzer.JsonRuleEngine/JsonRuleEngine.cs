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
        public delegate ILineNumberResolver BuildILineNumberResolver(TemplateContext context);

        private readonly BuildILineNumberResolver BuildLineNumberResolver;
        internal readonly IReadOnlyList<RuleDefinition> RuleDefinitions;

        /// <summary>
        /// Private constructor to enforce use of <see cref="JsonRuleEngine.Create(string, BuildILineNumberResolver)"/> for creating new instances.
        /// </summary>
        private JsonRuleEngine(List<RuleDefinition> rules, BuildILineNumberResolver jsonLineNumberResolverBuilder)
        {
            this.RuleDefinitions = rules;
            this.BuildLineNumberResolver = jsonLineNumberResolverBuilder;
        }

        /// <summary>
        /// Creates an instance of <see cref="JsonRuleEngine"/>.
        /// </summary>
        /// <param name="rawRuleDefinitions">The raw JSON rules to evaluate a template with.</param>
        /// <param name="jsonLineNumberResolverBuilder">A builder to create an <see cref="ILineNumberResolver"/> for mapping JSON paths from a
        /// processed template to the line number of the equivalent location in the original template.</param>
        public static JsonRuleEngine Create(string rawRuleDefinitions, BuildILineNumberResolver jsonLineNumberResolverBuilder)
        {
            if (jsonLineNumberResolverBuilder == null) throw new ArgumentNullException(nameof(jsonLineNumberResolverBuilder));

            try
            {
                return new JsonRuleEngine(ParseRuleDefinitions(rawRuleDefinitions), jsonLineNumberResolverBuilder);
            }
            catch (Exception e)
            {
                throw new JsonRuleEngineException($"Failed to parse rules.", e);
            }
        }

        /// <summary>
        /// Evaluates a template using rules defined in JSON.
        /// </summary>
        /// <param name="templateContext">The template context to evaluate.</param>
        /// <returns>The results of the rules against the template.</returns>
        public IEnumerable<IEvaluation> EvaluateTemplate(TemplateContext templateContext)
        {
            foreach (RuleDefinition rule in RuleDefinitions)
            {
                JsonRuleEvaluation evaluation = rule.Expression.Evaluate(
                    new JsonPathResolver(
                        templateContext.ExpandedTemplate,
                        templateContext.ExpandedTemplate.Path),
                    this.BuildLineNumberResolver(templateContext));

                 evaluation.RuleDefinition = rule;
                 evaluation.FileIdentifier = templateContext.TemplateIdentifier;
                    
                yield return evaluation;
            }
        }

        /// <summary>
        /// Parses <see cref="RuleDefinition"/>s from the provided JSON string.
        /// </summary>
        /// <param name="rawRuleDefinitions">The raw JSON rules to parse.</param>
        /// <returns>A list of <see cref="RuleDefinition"/>s.</returns>
        private static List<RuleDefinition> ParseRuleDefinitions(string rawRuleDefinitions)
        {
            if (rawRuleDefinitions == null) throw new ArgumentNullException(nameof(rawRuleDefinitions));
            if (string.IsNullOrWhiteSpace(rawRuleDefinitions)) throw new ArgumentException("String cannot be only whitespace.", nameof(rawRuleDefinitions));

            List<RuleDefinition> rules;

            try
            {
                rules = JsonConvert.DeserializeObject<List<RuleDefinition>>(rawRuleDefinitions);
            }
            catch (Exception e)
            {
                throw new JsonRuleEngineException("Failed to parse rule definitions.", e);
            }

            string currentRule = null;
            try
            {
                foreach (var rule in rules)
                {
                    currentRule = rule.Id;
                    rule.Expression = rule.ExpressionDefinition.ToExpression();
                }
            }
            catch (Exception e)
            {
                throw new JsonRuleEngineException($"Failed to initialize rule {currentRule}.", e);
            }

            return rules;
        }
    }
}
