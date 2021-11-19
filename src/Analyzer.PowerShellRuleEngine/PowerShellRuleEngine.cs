// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.Types;

using Powershell = System.Management.Automation.PowerShell; // There's a conflict between this class name and a namespace

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <summary>
    /// Executes template analysis encoded in PowerShell
    /// </summary>
    public class PowerShellRuleEngine : IRuleEngine
    {
        /// <summary>
        /// Execution environment for PowerShell
        /// </summary>
        private readonly Powershell powerShell;

        /// <summary>
        /// Regex that matches a string like: " on line: aNumber"
        /// </summary>
        private readonly Regex lineNumberRegex = new(@"\son\sline:\s\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Creates a new instance of a PowerShellRuleEngine
        /// </summary>
        public PowerShellRuleEngine()
        {
            this.powerShell = Powershell.Create();

            powerShell.Commands.AddCommand("Import-Module")
                .AddParameter("Name", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\TTK\arm-ttk.psd1"); // arm-ttk is added to the needed project's bins directories in build time
            powerShell.AddStatement();

            powerShell.Invoke();
        }

        /// <summary>
        /// Analyzes a template against the rules encoded in PowerShell.
        /// </summary>
        /// <param name="templateContext">The context of the template under analysis.
        /// <see cref="TemplateContext.TemplateIdentifier"/> must be the file path of the template to evaluate.</param>
        /// <returns>The <see cref="IEvaluation"/>s of the PowerShell rules against the template.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(TemplateContext templateContext)
        {
            if (templateContext?.TemplateIdentifier == null)
            {
                throw new ArgumentException($"{nameof(TemplateContext.TemplateIdentifier)} must not be null.", nameof(templateContext));
            }

            this.powerShell.Commands.AddCommand("Test-AzTemplate")
                .AddParameter("Test", "deploymentTemplate")
                .AddParameter("TemplatePath", templateContext.TemplateIdentifier);

            var executionResults = this.powerShell.Invoke();

            var evaluations = new List<PowerShellRuleEvaluation>();

            foreach (dynamic executionResult in executionResults)
            {
                var uniqueErrors = new Dictionary<string, SortedSet<int>>(); // Maps error messages to a sorted set of line numbers

                foreach (dynamic warning in executionResult.Warnings)
                {
                    PreProcessErrors(warning, uniqueErrors);
                }

                foreach (dynamic error in executionResult.Errors)
                {
                    PreProcessErrors(error, uniqueErrors);
                }

                foreach (KeyValuePair<string, SortedSet<int>> uniqueError in uniqueErrors)
                {
                    var evaluationResults = new List<PowerShellRuleResult>();
                    foreach (int lineNumber in uniqueError.Value)
                    {
                        evaluationResults.Add(new PowerShellRuleResult(false, lineNumber));
                    }

                    var ruleId = (executionResult.Name as string)?.Replace(" ", "") ?? string.Empty;
                    var ruleDescription = executionResult.Name + ". " + uniqueError.Key;
                    evaluations.Add(new PowerShellRuleEvaluation(ruleId, ruleDescription, false, evaluationResults));
                }
            }

            return evaluations;
        }

        private void PreProcessErrors(dynamic error, Dictionary<string, SortedSet<int>> uniqueErrors)
        {
            var lineNumber = 0;

            Type errorType = error.GetType();
            IEnumerable<PropertyInfo> errorProperties = errorType.GetRuntimeProperties();
            if (errorProperties.Where(prop => prop.Name == "TargetObject").Any())
            {
                if (error.TargetObject is PSObject targetObject && targetObject.Properties["lineNumber"] != null)
                {
                    lineNumber = error.TargetObject.lineNumber;
                }
            }

            var errorMessage = lineNumberRegex.Replace(error.ToString(), string.Empty); 

            if (!uniqueErrors.TryAdd(errorMessage, new SortedSet<int> { lineNumber }))
            {
                // errorMessage was already added to the dictionary
                uniqueErrors[errorMessage].Add(lineNumber);
            }
        }
    }
}