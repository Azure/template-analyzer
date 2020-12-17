// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Armory.TemplateProcessor;
using Armory.Types;
using Armory.Utilities;
using Newtonsoft.Json.Linq;

namespace Armory.Core
{
    /// <summary>
    /// This class runs the ARMory logic given the template and parameters passed to it
    /// </summary>
    public class Armory
    {
        private string Template { get; }
        private string Parameters { get; }

        /// <summary>
        /// Creates a new instance of ARMory
        /// </summary>
        /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
        public Armory(string template, string parameters = null)
        {
            this.Template = template ?? throw new ArgumentNullException(paramName: template);
            this.Parameters = parameters;
        }

        /// <summary>
        /// Runs the ARMory logic given the template and parameters passed to it
        /// </summary>
        /// <returns>List of ARMory results</returns>
        public IEnumerable<IResult> EvaluateRulesAgainstTemplate()
        {
            JToken templatejObject;

            try
            {
                ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(Template);
                templatejObject = armTemplateProcessor.ProcessTemplate(Parameters);
            }
            catch (Exception e)
            {
                throw new ArmoryException("Error while processing template.", e);
            }

            if (templatejObject == null)
            {
                throw new ArmoryException("Processed Template cannot be null.");
            }

            IEnumerable<IResult> results;
            try
            {
                var rules = LoadRules();
                var jsonRuleEngine = new JsonEngine.JsonRuleEngine();

                results = jsonRuleEngine.EvaluateRules(new TemplateContext { OriginalTemplate = JObject.Parse(Template), ExpandedTemplate = templatejObject, IsMainTemplate = true }, rules);
            }
            catch (Exception e)
            {
                throw new ArmoryException("Error while evaluating rules.", e);
            }

            return results;
        }

        private static string LoadRules()
        {
            return System.IO.File.ReadAllText("Armory.Rules\\BuiltInRules.json");
        }
    }
}
