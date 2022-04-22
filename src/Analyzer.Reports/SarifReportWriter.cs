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
        /// <summary>
        /// TODO
        /// </summary>
        public SarifLogger sarifLogger;

        internal const string UriBaseIdString = "ROOTPATH";
        internal const string PeriodString = ".";

        private IFileInfo reportFile;
        private Stream reportFileStream;
        private StreamWriter outputTextWriter;
        private Run sarifRun;
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
            this.reportFileStream = this.reportFile.Create();
            this.outputTextWriter = new StreamWriter(this.reportFileStream);
            this.rulesDictionary = new ConcurrentDictionary<string, ReportingDescriptor>();
            this.InitRun();
            this.RootPath = targetPath;

            this.sarifLogger = new SarifLogger(
                textWriter: this.outputTextWriter,
                logFilePersistenceOptions: LogFilePersistenceOptions.PrettyPrint | LogFilePersistenceOptions.OverwriteExistingOutputFile,
                tool: this.sarifRun.Tool,
                run: this.sarifRun,
                levels: new List<FailureLevel> { FailureLevel.Warning, FailureLevel.Error, FailureLevel.Note },
                kinds: new List<ResultKind> { ResultKind.Fail });
        }

        /// <inheritdoc/>
        public void WriteResults(IEnumerable<IEvaluation> evaluations, IFileInfo templateFile, IFileInfo parameterFile = null)
        {
            this.RootPath ??= templateFile.DirectoryName;
            bool isFileInRootPath = IsSubPath(this.rootPath, templateFile.FullName);
            string filePath = isFileInRootPath ?
                Path.GetRelativePath(this.RootPath, templateFile.FullName) :
                templateFile.FullName;

            foreach (var evaluation in evaluations.Where(eva => !eva.Passed))
            {
                // get rule definition from first level evaluation
                this.ExtractRule(evaluation);

                // Log result
                sarifLogger.Log(this.rulesDictionary[evaluation.RuleId], new Result
                {
                    RuleId = evaluation.RuleId,
                    Level = GetLevelFromEvaluation(evaluation),
                    Message = new Message { Id = "default" }, // should be customized message for each result 
                    Locations = ExtractLocations(evaluation, filePath, isFileInRootPath).Values.ToArray()
                });
            }
        }

        internal String RootPath
        { 
            get => this.rootPath; 
            set
            {
                if (string.IsNullOrWhiteSpace(rootPath) && !string.IsNullOrWhiteSpace(value))
                {
                    this.rootPath = value;
                    if (!this.sarifRun.OriginalUriBaseIds.ContainsKey(UriBaseIdString))
                    {
                        this.sarifRun.OriginalUriBaseIds.Add(
                            UriBaseIdString,
                            new ArtifactLocation { Uri = new Uri(UriHelper.MakeValidUri(rootPath), UriKind.RelativeOrAbsolute) });
                    }
                }
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
                },
                OriginalUriBaseIds = new Dictionary<string, ArtifactLocation>(),
            };
        }

        private void ExtractRule(IEvaluation evaluation)
        {
            if (!rulesDictionary.ContainsKey(evaluation.RuleId))
            {
                var hasUri = Uri.TryCreate(evaluation.HelpUri, UriKind.RelativeOrAbsolute, out Uri uri);
                rulesDictionary.Add(evaluation.RuleId, new ReportingDescriptor
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
        }

        private Dictionary<int, Location> ExtractLocations(IEvaluation evaluation, string filePath, bool pathBelongsToRoot, Dictionary<int, Location> locations = null)
        {
            locations ??= new Dictionary<int, Location>();
            if (evaluation.Result != null && !evaluation.Result.Passed)
            {
                var line = evaluation.Result.LineNumber;
                if (!locations.ContainsKey(line))
                {
                    locations[line] = new Location
                    {
                        PhysicalLocation = new PhysicalLocation
                        {
                            ArtifactLocation = new ArtifactLocation
                            {
                                Uri = new Uri(
                                    UriHelper.MakeValidUri(filePath),
                                    UriKind.RelativeOrAbsolute),
                                UriBaseId = pathBelongsToRoot ? UriBaseIdString : null,
                            },
                            Region = new Region { StartLine = line },
                        },
                    };
                }
            }

            foreach (var eval in evaluation.Evaluations.Where(e => !e.Passed))
            {
                this.ExtractLocations(eval, filePath, pathBelongsToRoot, locations);
            }

            return locations;
        }

        private FailureLevel GetLevelFromEvaluation(IEvaluation evaluation)
        {
            // The rule severity definition work item: https://github.com/Azure/template-analyzer/issues/177
            return evaluation.Passed ? FailureLevel.Note : FailureLevel.Error;
        }

        internal static bool IsSubPath(string rootPath, string childFilePath)
        {
            var relativePath = Path.GetRelativePath(rootPath, childFilePath);
            return !relativePath.StartsWith('.') && !Path.IsPathRooted(relativePath);
        }

        internal static string AppendPeriod(string text) =>
            text == null ? string.Empty :
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
            this.sarifLogger?.Dispose();
            this.outputTextWriter?.Dispose();
            this.reportFileStream?.Dispose();
        }
    }
}
