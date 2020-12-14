// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Armory.TemplateProcessor;
using Armory.Types;
using Newtonsoft.Json.Linq;

namespace Armory.Core
{
    /// <summary>
    /// This class runs the ARMory logic given the template and parameters passed to it
    /// </summary>
    public class Runner
    {
        /// <summary>
        /// Runs the ARMory logic given the template and parameters passed to it
        /// </summary>
        /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
        /// <returns></returns>
        public static IEnumerable<IResult> Run(string template, string parameters = null)
        {
            if (template == null)
            {
                throw new ArgumentNullException(template);
            }

            JToken templatejObject = JObject.Parse(template);

            var rules = LoadRules();
            var jsonRuleEngine = new JsonEngine.JsonRuleEngine();

            var results = jsonRuleEngine.Run(new TemplateContext { OriginalTemplate = JObject.Parse(template), ExpandedTemplate = templatejObject, IsMainTemplate = true }, rules);

            return results;
        }

        private static string LoadRules()
        {
            return System.IO.File.ReadAllText("Armory.Rules\\BuiltInRules.json");
        }
    }
}
