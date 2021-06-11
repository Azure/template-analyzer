// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <summary>
    /// Executes template analysis encoded in PowerShell
    /// </summary>
    public class PowerShellRuleEngine
    {
        /// <summary>
        /// Execution environment for PowerShell
        /// </summary>
        private readonly System.Management.Automation.PowerShell PowerShell;

        /// <summary>
        /// Regex that matches a string like: " on line: aNumber"
        /// </summary>
        private readonly Regex LineNumberRegex = new(@"\son\sline:\s\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Creates a new instance of a PowerShellRuleEngine
        /// </summary>
        public PowerShellRuleEngine()
        {
            this.PowerShell = System.Management.Automation.PowerShell.Create();

            PowerShell.Commands.AddCommand("Set-ExecutionPolicy")
                .AddParameter("Scope", "Process") // Affects only the current PowerShell session
                .AddParameter("ExecutionPolicy", "Unrestricted");
            PowerShell.AddStatement();

            PowerShell.Commands.AddCommand("Import-Module")
                .AddParameter("Name", @".\TTK\arm-ttk.psd1"); // arm-ttk is added to the needed project's bins directories in build time 
            PowerShell.AddStatement();

            PowerShell.Invoke();
        }

        /// <summary>
        /// Evaluates template against the rules encoded in PowerShell, and outputs the results to the console
        /// </summary>
        /// <param name="templateFilePath">The file path of the template under analysis.</param>
        public IEnumerable<IEvaluation> EvaluateRules(string templateFilePath)
        {
            this.PowerShell.Commands.AddCommand("Test-AzTemplate")
                .AddParameter("Test", "deploymentTemplate")
                .AddParameter("TemplatePath", templateFilePath);

            var executionResults = this.PowerShell.Invoke();

            var evaluations = new List<PowerShellRuleEvaluation>();

            foreach (dynamic executionResult in executionResults)
            {
                var uniqueErrors = new Dictionary<string, SortedSet<int>>(); // Maps error messages to a sorted set of line numbers

                foreach (dynamic warning in executionResult.Warnings)
                {
                    AddErrorToDictionary(warning, ref uniqueErrors);
                }

                foreach (dynamic error in executionResult.Errors)
                {
                    AddErrorToDictionary(error, ref uniqueErrors);
                }

                foreach (KeyValuePair<string, SortedSet<int>> uniqueError in uniqueErrors)
                {
                    var evaluationResults = new List<PowerShellRuleResult>();
                    foreach (int lineNumber in uniqueError.Value)
                    {
                        evaluationResults.Add(new PowerShellRuleResult(false, lineNumber));
                    }
                    evaluations.Add(new PowerShellRuleEvaluation(executionResult.Name, uniqueError.Key, false, evaluationResults));
                }
            }

            return evaluations;
        }

        private void AddErrorToDictionary(dynamic error, ref Dictionary<string, SortedSet<int>> uniqueErrors)
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

            var errorMessage = LineNumberRegex.Replace(error.ToString(), string.Empty); 

            if (!uniqueErrors.TryAdd(errorMessage, new SortedSet<int> { lineNumber }))
            {
                // errorMessage was already added to the dictionary
                uniqueErrors[errorMessage].Add(lineNumber);
            }
        }
    }
}