// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <summary>
    /// Executes template analysis encoded in PowerShell
    /// </summary>
    public class PowerShellRuleEngine
    {
        /// <summary>
        /// Evaluates template against the rules encoded in PowerShell, and outputs the results to the console
        /// </summary>
        /// <param name="templateFilePath">The file path of the template under analysis.</param>
        public static IEnumerable<IEvaluation> EvaluateRules(string templateFilePath)
        {
            var powerShell = System.Management.Automation.PowerShell.Create();

            powerShell.Streams.Error.DataAdded += (sender, e) => HandleDataAddedInStreams(powerShell.Streams.Error[e.Index], ConsoleColor.Red);
            powerShell.Streams.Warning.DataAdded += (sender, e) => HandleDataAddedInStreams(powerShell.Streams.Warning[e.Index], ConsoleColor.Yellow);
            powerShell.Streams.Information.DataAdded += (sender, e) => HandleDataAddedInStreams(powerShell.Streams.Information[e.Index]);
            powerShell.Streams.Verbose.DataAdded += (sender, e) => HandleDataAddedInStreams(powerShell.Streams.Verbose[e.Index]);
            powerShell.Streams.Debug.DataAdded += (sender, e) => HandleDataAddedInStreams(powerShell.Streams.Debug[e.Index]);

            powerShell.Commands.AddCommand("Set-ExecutionPolicy")
                .AddParameter("Scope", "Process") // Affects only the current PowerShell session
                .AddParameter("ExecutionPolicy", "Unrestricted");
            powerShell.AddStatement();

            powerShell.Commands.AddCommand("Import-Module")
                .AddParameter("Name", @"..\..\..\..\Analyzer.PowerShellRuleEngine\bin\TTK\arm-ttk\arm-ttk.psd1"); // arm-ttk is added to the project's bin directory in build time 
            powerShell.AddStatement();

            powerShell.Commands.AddCommand("Test-AzTemplate")
                .AddParameter("Test", "deploymentTemplate")
                .AddParameter("TemplatePath", templateFilePath);

            var executionResults = powerShell.Invoke();

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
                        var evaluationResult = new PowerShellRuleResult(false, lineNumber);
                        evaluationResults.Add(evaluationResult);
                    }
                    var evaluation = new PowerShellRuleEvaluation(executionResult.Name, uniqueError.Key, false, evaluationResults);
                    evaluations.Add(evaluation);
                }
            }

            return evaluations;
        }

        private static void AddErrorToDictionary(dynamic error, ref Dictionary<string, SortedSet<int>> uniqueErrors)
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

            var lineNumberRegex = new Regex(@"\son\sline:\s\d+");
            var errorMessage = lineNumberRegex.Replace(error.ToString(), string.Empty); 

            if (uniqueErrors.ContainsKey(errorMessage))
            {
                uniqueErrors[errorMessage].Add(lineNumber);
            }
            else
            {
                uniqueErrors[errorMessage] = new SortedSet<int> { lineNumber };
            }
        }

        static void HandleDataAddedInStreams(object newData, ConsoleColor? color = null)
        {
            if (color.HasValue) {
                Console.ForegroundColor = color.Value;
            }

            Console.WriteLine(newData);

            Console.ResetColor();
        }
    }
}