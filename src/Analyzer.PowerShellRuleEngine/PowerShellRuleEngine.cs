// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;
using System;
using System.Collections.Generic;
using System.Management.Automation;

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
                .AddParameter("Name", @"..\..\..\..\Analyzer.PowerShellRuleEngine\bin\Debug\netstandard2.1\TTK\arm-ttk.psd1"); // arm-ttk is added to the project's bin directory in build time 
            powerShell.AddStatement();

            powerShell.Commands.AddCommand("Test-AzTemplate")
                .AddParameter("Test", "deploymentTemplate")
                .AddParameter("TemplatePath", templateFilePath);

            var executionResults = powerShell.Invoke();

            var evaluations = new List<PowerShellRuleEvaluation>();

            foreach (dynamic executionResult in executionResults)
            {
                var evaluationResults = new List<PowerShellRuleResult>();
                var extraInfo = ""; // Temporal until the JSON engine also reports more info like variable names, warnings

                foreach (dynamic warning in executionResult.Warnings)
                {
                    extraInfo = extraInfo + "Warning: " + warning.ToString() + ". ";
                }

                foreach (dynamic error in executionResult.Errors)
                {
                    var lineNumber = 0;
                    if (error.TargetObject is PSObject targetObject && targetObject.Properties["lineNumber"] != null)
                    {
                        lineNumber = error.TargetObject.lineNumber;
                    }

                    var evaluationResult = new PowerShellRuleResult(executionResult.Passed, lineNumber);
                    evaluationResults.Add(evaluationResult);

                    extraInfo = extraInfo + error.ToString() + ". ";
                }
                    
                var evaluation = new PowerShellRuleEvaluation(executionResult.Name, extraInfo, executionResult.Passed, evaluationResults);
                evaluations.Add(evaluation);
            }

            return evaluations;
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