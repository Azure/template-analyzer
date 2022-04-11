// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;
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
        internal IReadOnlyList<RuleDefinition> RuleDefinitions;

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
            if (rawRuleDefinitions == null) throw new ArgumentNullException(nameof(rawRuleDefinitions));
            if (string.IsNullOrWhiteSpace(rawRuleDefinitions)) throw new ArgumentException("String cannot be only whitespace.", nameof(rawRuleDefinitions));
            if (jsonLineNumberResolverBuilder == null) throw new ArgumentNullException(nameof(jsonLineNumberResolverBuilder));

            return new JsonRuleEngine(ParseRuleDefinitions(rawRuleDefinitions), jsonLineNumberResolverBuilder);
        }

        /// <summary>
        /// Modifies the rules to run based on values defined in the configurations file.
        /// </summary>
        /// <param name="configuration">The configuration specifying rule modifications.</param>
        public void FilterRules(string configuration)
        {
            if (string.IsNullOrEmpty(configuration))
                return;

            ConfigurationDefinition contents;
            try
            {
                contents = JsonConvert.DeserializeObject<ConfigurationDefinition>(configuration);
            }
            catch (Exception e)
            {
                throw new JsonRuleEngineException($"Failed to parse configurations file.", e);
            }

            if (contents.InclusionsConfigurationDefinition != null)
            {
                var includeSeverities = contents.InclusionsConfigurationDefinition.Severity;
                var includeIds = contents.InclusionsConfigurationDefinition.Ids;

                RuleDefinitions = RuleDefinitions.Where(r => (includeSeverities != null && includeSeverities.Contains(r.Severity)) ||
                    (includeIds != null && includeIds.Contains(r.Id))).ToList().AsReadOnly();
            }
            else if (contents.ExclusionsConfigurationDefinition != null)
            {
                var excludeSeverities = contents.ExclusionsConfigurationDefinition.Severity;
                var excludeIds = contents.ExclusionsConfigurationDefinition.Ids;

                RuleDefinitions = RuleDefinitions.Where(r => !(excludeSeverities != null && excludeSeverities.Contains(r.Severity)) &&
                    !(excludeIds != null && excludeIds.Contains(r.Id))).ToList().AsReadOnly();
            }

            if (contents.SeverityOverrides != null)
            {                
                var ruleSubset = RuleDefinitions.Where(r => contents.SeverityOverrides.Keys.Contains(r.Id));
                foreach (RuleDefinition rule in ruleSubset)
                {
                    rule.Severity = contents.SeverityOverrides[rule.Id];
                }
            }
        }

        /// <summary>
        /// Analyzes a template using rules defined in JSON.
        /// </summary>
        /// <param name="templateContext">The template context to analyze.</param>
        /// <param name="logger">A logger to report errors and debug information</param>
        /// <returns>The results of the rules against the template.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(TemplateContext templateContext, ILogger logger = null)
        {
            foreach (RuleDefinition rule in RuleDefinitions)
            {
                logger?.LogDebug("Evaluating rule {ruleID} in the JSON rule engine", rule.Id);

                var evaluations = rule.Expression.Evaluate(
                    new JsonPathResolver(
                        templateContext.ExpandedTemplate,
                        templateContext.ExpandedTemplate.Path),
                    this.BuildLineNumberResolver(templateContext));

                foreach (var evaluation in evaluations)
                {
                    evaluation.RuleDefinition = rule;
                    evaluation.FileIdentifier = templateContext.TemplateIdentifier;

                    yield return evaluation;
                }
            }
        }

        /// <summary>
        /// Parses <see cref="RuleDefinition"/>s from the provided JSON string.
        /// </summary>
        /// <param name="rawRuleDefinitions">The raw JSON rules to parse.</param>
        /// <returns>A list of <see cref="RuleDefinition"/>s.</returns>
        private static List<RuleDefinition> ParseRuleDefinitions(string rawRuleDefinitions)
        {
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
