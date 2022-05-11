// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
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
                initialState.ExecutionPolicy = PowerShell.ExecutionPolicy.Unrestricted;
            }

            // Create Powershell runtime
            var powershell = Powershell.Create(initialState);

            // Unblock scripts if running a DEBUG build
            UnblockScripts(powershell);

            // Import PSRule modules
            powershell.AddCommand("Import-Module").AddParameter("Name", ModuleManifestPath("PSRule"))
                .AddStatement().AddCommand("Import-Module").AddParameter("Name", ModuleManifestPath("PSRule.Rules.Azure", "-nodeps"))
                .Invoke();

            if (!powershell.HadErrors)
            {
                // Save the runspace with TTK loaded
                this.runspace = powershell.Runspace;
            }
        }

        /// <summary>
        /// Get the module manifest.
        /// </summary>
        private static string ModuleManifestPath(string name, string suffix = "")
        {
            return Path.Combine(ModulePath(name), $"{name}{suffix}.psd1");
        }

        /// <summary>
        /// Get module path regardless of the current version.
        /// </summary>
        private static string ModulePath(string name)
        {
            var basePath = Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(PowerShellRuleEngine)).Location), name);
            return Directory.EnumerateDirectories(basePath).FirstOrDefault();
        }

        [Conditional("DEBUG")]
        private void UnblockScripts(Powershell powershell)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                powershell
                    .AddCommand("Unblock-File").AddParameter("Path", Path.Combine(ModulePath("PSRule"), "*"))
                    .AddStatement().AddCommand("Unblock-File").AddParameter("Path", Path.Combine(ModulePath("PSRule.Rules.Azure"), "*"))
                    .Invoke();

                if (powershell.HadErrors)
                {
                    throw new Exception("Unable to unblock scripts");
                }

                powershell.Commands.Clear();
            }
        }

        /// <summary>
        /// Analyzes a template against the rules encoded in PowerShell.
        /// </summary>
        /// <param name="templateContext">The context of the template under analysis.
        /// <see cref="TemplateContext.TemplateIdentifier"/> must be the file path of the template to evaluate.</param>
        /// <param name="logger">A logger to report errors and debug information</param>
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

                logger?.LogError("There was an error running the PowerShell based checks");

                return Enumerable.Empty<IEvaluation>();
            }

            // TODO: Temporary work-around: write template to disk so PSRule will analyze it
            var tempTemplateFile = Path.GetTempFileName();
            File.WriteAllText(tempTemplateFile, templateContext.ExpandedTemplate.ToString());
            var templateFile = tempTemplateFile.Replace(".tmp", ".json");
            File.Move(tempTemplateFile, templateFile);

            var powershell = Powershell.Create(runspace);

            // Run PSRule on full template, for template-level rules to execute.
            var executionResults = powershell
                .AddCommand("Invoke-PSRule")
                .AddParameter("Module", "PSRule.Rules.Azure-nodeps")
                .AddParameter("InputPath", templateFile)
                .AddParameter("Format", "File")
                .AddParameter("Outcome", "Fail,Error")
                .Invoke()
                .ToList();
            
            powershell.Commands.Clear();

            // Remove temporary file
            File.Delete(templateFile);

            // Run PSRule on resources array, for typed-rules.
            var resources = templateContext.ExpandedTemplate.InsensitiveToken("resources");
            executionResults.AddRange(powershell
                .AddCommand("Invoke-PSRule")
                .AddParameter("Module", "PSRule.Rules.Azure-nodeps")
                .AddParameter("InputObject", resources.ToString())
                .AddParameter("Format", "Json")
                .AddParameter("Outcome", "Fail,Error")
                .Invoke());

            var evaluations = new List<PowerShellRuleEvaluation>();

            foreach (var executionResult in executionResults.Cast<PSObject>())
            {
                if ("Error".Equals(executionResult.Properties["Outcome"].Value.ToString()))
                {
                    // Error running the rule.
                    // Using reflection for now since we don't have a reference to the PSRule libraries.
                    var errorInfo = executionResult.Properties["Error"].Value;
                    var message = GetMember<string>(errorInfo, "Message");
                    logger.LogError("Error running rule: {error}", message);
                    continue;
                }

                var ruleHelpInfo = executionResult.Properties["Info"].Value;

                var ruleId = GetMember<string>(ruleHelpInfo, "Name");
                var ruleDescription = GetMember<string>(ruleHelpInfo, "DisplayName");
                var recommendation = GetMember<string>(ruleHelpInfo, "Recommendation");

                var severity = executionResult.Properties["Level"].Value.ToString() switch
                {
                    "Error" => Severity.High,
                    "Warning" => Severity.Medium,
                    _ => Severity.Low
                };

                // Get source line
                int line = GetMember<int>((executionResult.Properties["Source"].Value as Array)?.GetValue(0), "Line");

                foreach (var reason in executionResult.Properties["Reason"]?.Value as string[] ?? Array.Empty<string>())
                {
                    // TODO: add reason as a message into result
                    evaluations.Add(
                        new PowerShellRuleEvaluation(ruleId, ruleDescription, recommendation,
                            templateContext.TemplateIdentifier, false, severity,
                            new PowerShellRuleResult(false, line)));
                }

            }

            return evaluations;
        }

        /// <summary>
        /// Temporary helper function to get property values
        /// from PSRule objects via reflection.
        /// </summary>
        /// <param name="data">The object to get values from.</param>
        /// <param name="memberName">The name of the property to get the value of.</param>
        /// <returns>The value of the named property from the passed data.</returns>
        private static T GetMember<T>(object data, string memberName) =>
            (T)data.GetType()
                .GetProperty(memberName, BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public)
                .GetValue(data);
    }
}