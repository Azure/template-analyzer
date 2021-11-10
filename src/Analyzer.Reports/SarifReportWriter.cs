// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Writers;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// Class to export analysis result to SARIF report
    /// </summary>
    public class SarifReportWriter : IReportWriter
    {
        internal const string UriBaseIdString = "ROOTPATH";
        internal const string PeriodString = ".";

        private IFileInfo reportFile;
        private Run sarifRun;
        private IList<Result> sarifResults;
        private IDictionary<string, ReportingDescriptor> rulesDictionary;
        private string rootPath;

        /// <summary>
        /// Constructor of the SarifReportWriter class
        /// </summary>
        /// <param name="reportFile">File where the report will be written</param>
        /// <param name="targetPath">The directory that will be analyzed</param>
        public SarifReportWriter(IFileInfo reportFile, string targetPath = null)
        {
            this.reportFile = reportFile ?? throw new ArgumentException(nameof(reportFile));
            this.InitRun();
            this.rulesDictionary = new ConcurrentDictionary<string, ReportingDescriptor>();
            this.sarifResults = new List<Result>();
            this.rootPath = targetPath;
        }

        /// <inheritdoc/>
        public void WriteResults(IEnumerable<IEvaluation> evaluations, IFileInfo templateFile, IFileInfo parameterFile = null)
        {
            this.rootPath ??= templateFile.DirectoryName;
            foreach (var evaluation in evaluations.Where(eva => !eva.Passed))
            {
                // get rule definition from first level evaluation
                ReportingDescriptor rule = this.ExtractRule(evaluation);
                this.ExtractResult(evaluation, evaluation, templateFile.FullName);
            }
        }

        internal Run SarifRun => this.sarifRun;

        private void InitRun()
        {
            this.sarifRun = new Run
            {
                Tool = new Tool
                {
                    Driver = new ToolComponent
                    {
                        Name = Constants.ToolName,
                        FullName = Constants.ToolFullName,
                        Version = Constants.ToolVersion,
                        InformationUri = new Uri(Constants.InformationUri),
                        Organization = Constants.Organization,
                    }
                }
            };
        }

        private ReportingDescriptor ExtractRule(IEvaluation evaluation)
        {
            if (!rulesDictionary.TryGetValue(evaluation.RuleId, out _))
            {
                var hasUri = Uri.TryCreate(evaluation.HelpUri, UriKind.RelativeOrAbsolute, out Uri uri);
                rulesDictionary.Add(
                    evaluation.RuleId,
                    new ReportingDescriptor
                    {
                        Id = evaluation.RuleId,
                        // Name = evaluation.RuleId, TBD issue #198
                        FullDescription = new MultiformatMessageString { Text = AppendPeriod(evaluation.RuleDescription) },
                        Help = new MultiformatMessageString { Text = AppendPeriod(evaluation.Recommendation) },
                        HelpUri = hasUri ? uri : null,
                        MessageStrings = new Dictionary<string, MultiformatMessageString>()
                        {
                            { "default", new MultiformatMessageString { Text = AppendPeriod(evaluation.RuleDescription) } }
                        },
                        DefaultConfiguration = new ReportingConfiguration { Level = GetLevelFromEvaluation(evaluation) }
                    });
            }
            return rulesDictionary[evaluation.RuleId];
        }

        private void ExtractResult(IEvaluation rootEvaluation, IEvaluation evaluation, string filePath)
        {
            foreach (var result in evaluation.Results.Where(r => !r.Passed))
            {
                this.sarifResults.Add(new Result
                {
                    RuleId = rootEvaluation.RuleId,
                    Level = GetLevelFromEvaluation(rootEvaluation),
                    Message = new Message { Id = "default" }, // should be customized message for each result 
                    Locations = new[]
                    {
                        new Location
                        {
                            PhysicalLocation = new PhysicalLocation
                            {
                                ArtifactLocation = new ArtifactLocation
                                {
                                    Uri = new Uri(
                                    UriHelper.MakeValidUri(
                                        filePath.Replace(this.rootPath, string.Empty, StringComparison.OrdinalIgnoreCase)),
                                    UriKind.Relative),
                                    UriBaseId = UriBaseIdString,
                                },
                                Region = new Region { StartLine = result.LineNumber },
                            },
                        },
                    }
                });
            }

            foreach (var eval in evaluation.Evaluations.Where(e => !e.Passed))
            {
                this.ExtractResult(rootEvaluation, eval, filePath);
            }
        }

        private FailureLevel GetLevelFromEvaluation(IEvaluation evaluation)
        {
            // The rule severity definition work item: https://github.com/Azure/template-analyzer/issues/177
            return evaluation.Passed ? FailureLevel.Note : FailureLevel.Error;
        }

        private void PersistReport()
        {
            using Stream outputTextStream = this.reportFile.Create();
            using var outputTextWriter = new StreamWriter(outputTextStream);
            using var sarifLogger = new SarifLogger(
                textWriter: outputTextWriter,
                logFilePersistenceOptions: LogFilePersistenceOptions.PrettyPrint | LogFilePersistenceOptions.OverwriteExistingOutputFile,
                tool: this.sarifRun.Tool,
                run: this.sarifRun,
                levels: new List<FailureLevel> { FailureLevel.Warning, FailureLevel.Error, FailureLevel.Note },
                kinds: new List<ResultKind> { ResultKind.Fail });
            {
                this.sarifRun.OriginalUriBaseIds = new Dictionary<string, ArtifactLocation>
                {
                    [UriBaseIdString] = new ArtifactLocation { Uri = new Uri(UriHelper.MakeValidUri(this.rootPath), UriKind.RelativeOrAbsolute) },
                };

                if (this.sarifResults != null)
                {
                    foreach (var result in this.sarifResults)
                    {
                        sarifLogger.Log(this.rulesDictionary[result.RuleId], result);
                    }
                }
            }
        }

        internal static string AppendPeriod(string text) => 
            text.EndsWith(PeriodString, StringComparison.OrdinalIgnoreCase) ? text : text + PeriodString;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources owned by this instance.
        /// </summary>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            this.PersistReport();
        }
    }
}
