// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation.Runspaces;

namespace Microsoft.Azure.Templates.Analyzer.Cli
{
    /// <summary>
    /// Executes template analysis encoded in PowerShell
    /// </summary>
    internal class PowerShellExecutor // TODO rename
    {
        /// <summary>
        /// Evaluates template against the rules encoded in PowerShell, and outputs the results to the console
        /// </summary>
        /// <param name="templateFullFilePath">The full file path of the template under analysis.</param>
        public static void EvaluateAndOutputResults(string templateFullFilePath) // TODO rename
        {
            using var runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();

            var powerShell = System.Management.Automation.PowerShell.Create(runspace); // FIXME namespace

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
                .AddParameter("Name", @"..\..\arm-ttk\arm-ttk\arm-ttk.psd1"); // arm-ttk is added to the project's bin directory in build time 
            powerShell.AddStatement();

            powerShell.Commands.AddCommand("Test-AzTemplate")
                .AddParameter("Test", "deploymentTemplate")
                .AddParameter("TemplatePath", templateFullFilePath);

            var results = powerShell.Invoke();

            foreach (dynamic result in results)
            {
                if (result != null)
                {
                    // TODO create Evaluation object and return that

                    Console.WriteLine(result.Name);
                    Console.WriteLine(result.Passed);

                    if (result.Errors.Count > 0)
                    {
                       foreach (dynamic error in result.Errors)
                       {
                            Console.WriteLine(error); // TODO line number
                            Console.WriteLine(error.TargetObject.Name);
                            Console.WriteLine(error.TargetObject.Value);
                        }
                    }

                    // TODO check warings too?
                }
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