// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.Extensions.Logging;

using Powershell = System.Management.Automation.PowerShell; // There's a conflict between this class name and a namespace

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine
{
    /// <summary>
    /// Executes template analysis encoded in PowerShell.
    /// </summary>
    public class PowerShellRuleEngine : IRuleEngine
    {
        /// <summary>
        /// Execution environment for PowerShell.
        /// </summary>
        private readonly Runspace runspace;

        /// <summary>
        /// Logger for logging notable events.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Regex that matches a string like: " on line: aNumber".
        /// </summary>
        private readonly Regex lineNumberRegex = new(@"\son\sline:\s\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // TODO: Download and output modules with the build
        private const string psruleLocation = @"..\..\..\..\Analyzer.PowerShellRuleEngine\psrule.2.0.1";
        private const string psruleAzureLocation = @"..\..\..\..\Analyzer.PowerShellRuleEngine\psrule.rules.azure.1.14.3";

        /// <summary>
        /// Creates a new instance of a PowerShellRuleEngine.
        /// </summary>
        /// <param name="logger">A logger to report errors and debug information.</param>
        public PowerShellRuleEngine(ILogger logger = null)
        {
            this.logger = logger;

            try
            {
                // There are 2 different 'Default' functions available:
                // https://docs.microsoft.com/en-us/powershell/scripting/developer/hosting/creating-an-initialsessionstate?view=powershell-7.2
                // CreateDefault2 appears to not have a dependency on Microsoft.Management.Infrastructure.dll,
                // which is missing when publishing for 'win-x64', and PowerShell throws an exception creating the InitialSessionState.
                // Notably, Microsoft.Management.Infrastructure.dll is available when publishing for specific Windows versions (such as win7-x64),
                // but since this libary is not needed here, might as well just eliminate the dependency.
                var initialState = InitialSessionState.CreateDefault2();

                if (Platform.IsWindows)
                {
                    // Ensure we can execute the signed bundled scripts.
                    // (This sets the policy at the Process scope.)
                    // When custom PS rules are supported, we may need to update this to be more relaxed.
                    initialState.ExecutionPolicy = PowerShell.ExecutionPolicy.RemoteSigned; // TODO or Unrestricted?
                }

                var powershell = Powershell.Create(initialState);

                // Scripts that aren't unblocked will prompt for permission to run on Windows before executing,
                // even if the scripts are signed.  (Unsigned scripts simply won't run.)
                UnblockScripts(powershell, psruleLocation);
                UnblockScripts(powershell, psruleAzureLocation);

                // Import PSRule modules
                powershell.AddCommand("Import-Module").AddParameter("Name", Path.Combine(psruleLocation, "PSRule.psd1"))
                .AddStatement().AddCommand("Import-Module").AddParameter("Name", Path.Combine(psruleAzureLocation, "PSRule.Rules.Azure.psd1"))
                .Invoke();

                if (!powershell.HadErrors)
                {
                    // Save the runspace with TTK loaded
                    this.runspace = powershell.Runspace;
                }
                else
                {
                    LogPowerShellErrors(powershell.Streams.Error, "There was an error initializing TTK.");
                }
            }
            catch (Exception e)
            {
                this.logger?.LogError(e, "There was an exception while initializing TTK.");
            }
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

            if (runspace == null)
            {
                // There was an error loading the TTK module.  Return an empty collection.
                logger?.LogWarning("Unable to run PowerShell based checks.  Initialization failed.");
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
                .AddParameter("Module", "PSRule.Rules.Azure")
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
                .AddParameter("Module", "PSRule.Rules.Azure")
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

                foreach (var reason in executionResult.Properties["Reason"]?.Value as string[] ?? Array.Empty<string>())
                {
                    // TODO: add reason as a message into result
                    evaluations.Add(
                        new PowerShellRuleEvaluation(ruleId, ruleDescription, recommendation,
                            templateContext.TemplateIdentifier, false, severity,
                            new PowerShellRuleResult(false, 1)));
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

        /// <summary>
        /// Unblocks scripts on Windows to allow them to run.
        /// If a script is not unblocked, even if it's signed,
        /// PowerShell prompts for confirmation before executing.
        /// This prompting would throw an exception, because there's
        /// no interaction with a user that would allow for confirmation.
        /// </summary>
        private void UnblockScripts(Powershell powershell, string directory)
        {
            if (Platform.IsWindows)
            {
                powershell
                    .AddCommand("Get-ChildItem")
                    .AddParameter("Path", directory)
                    .AddParameter("Recurse")
                    .AddCommand("Unblock-File")
                    .Invoke();

                if (powershell.HadErrors)
                {
                    LogPowerShellErrors(powershell.Streams.Error, $"There was an error unblocking scripts in path '{directory}'.");
                }

                powershell.Commands.Clear();
            }
        }

        private void LogPowerShellErrors(PSDataCollection<ErrorRecord> errors, string summary)
        {
            this.logger?.LogError(summary);
            foreach (var error in errors)
            {
                this.logger?.LogError(error.ErrorDetails.Message);
            }
        }
    }
}