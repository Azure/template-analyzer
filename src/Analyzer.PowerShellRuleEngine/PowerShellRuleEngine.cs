// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Extensions.Logging;

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
        private readonly Runspace runspace;

        /// <summary>
        /// Regex that matches a string like: " on line: aNumber"
        /// </summary>
        private readonly Regex lineNumberRegex = new(@"\son\sline:\s\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Creates a new instance of a PowerShellRuleEngine
        /// </summary>
        public PowerShellRuleEngine()
        {
            var initialState = InitialSessionState.CreateDefault();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Ensure we can execute the signed bundled scripts.
                // (This sets the policy at the Process scope.)
                // When custom PS rules are supported, we may need to update this to be more relaxed.
                initialState.ExecutionPolicy = PowerShell.ExecutionPolicy.RemoteSigned;
            }

            // Import ARM-TTK module.
            // It's copied to the bin directory as part of the build process.
            var powershell = Powershell.Create(initialState);
            powershell.AddCommand("Import-Module")
                .AddParameter("Name", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\TTK\arm-ttk.psd1")
                .Invoke();

            if (!powershell.HadErrors)
            {
                // Save the runspace with TTK loaded
                this.runspace = powershell.Runspace; 
            }
        }

        /// <summary>
        /// Analyzes a template against the rules encoded in PowerShell.
        /// </summary>
        /// <param name="templateContext">The context of the template under analysis.
        /// <param name="logger">A logger to report errors and debug information</param>
        /// <see cref="TemplateContext.TemplateIdentifier"/> must be the file path of the template to evaluate.</param>
        /// <returns>The <see cref="IEvaluation"/>s of the PowerShell rules against the template.</returns>
        public IEnumerable<IEvaluation> AnalyzeTemplate(TemplateContext templateContext, ILogger logger = null)
        {
            if (templateContext?.TemplateIdentifier == null)
            {
                throw new ArgumentException($"{nameof(TemplateContext.TemplateIdentifier)} must not be null.", nameof(templateContext));
            }

            if (runspace == null)
            {
                // There was an error loading the TTK module.  Return an empty collection.
                return Enumerable.Empty<IEvaluation>();
            }

            var executionResults = Powershell.Create(runspace)
                .AddCommand("Test-AzTemplate")
                .AddParameter("Test", "deploymentTemplate")
                .AddParameter("TemplatePath", templateContext.TemplateIdentifier)
                .Invoke();

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
                    var ruleId = (executionResult.Name as string)?.Replace(" ", "");
                    ruleId = !String.IsNullOrEmpty(ruleId) ? ruleId : "TTK";
                    var ruleDescription = executionResult.Name + ". " + uniqueError.Key;

                    foreach (int lineNumber in uniqueError.Value)
                    {
                        evaluations.Add(new PowerShellRuleEvaluation(ruleId, ruleDescription, false, new PowerShellRuleResult(false, lineNumber)));
                    }
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