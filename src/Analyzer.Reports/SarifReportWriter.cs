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

using SarifResult = Microsoft.CodeAnalysis.Sarif.Result;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// Class to export analysis result to SARIF report
    /// </summary>
    public class SarifReportWriter : IReportWriter
    {
        internal const string UriBaseIdString = "ROOTPATH";
        internal const string PeriodString = ".";

        private readonly List<string> filesAlreadyOutput = new List<string>();
        private readonly IFileInfo reportFile;
        private readonly Stream reportFileStream;
        private readonly StreamWriter outputTextWriter;
        private Run sarifRun;
        private readonly IDictionary<string, ReportingDescriptor> rulesDictionary;
        private string rootPath;
        private int totalResults = 0;


        /// <summary>
        /// Logger used to output information to the SARIF file
        /// </summary>
        public SarifLogger SarifLogger { get; set; }

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

            this.SarifLogger = new SarifLogger(
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

            var resultsByFile = ReportsHelper.GetResultsByFile(evaluations, filesAlreadyOutput);

            // output files in sorted order, but always output root first
            var filesWithResults = resultsByFile.Keys.ToList();
            filesWithResults.Sort();

            int rootIndex = filesWithResults.IndexOf(templateFile.FullName);
            if (rootIndex != -1)
            {
                filesWithResults.RemoveAt(rootIndex);
                filesWithResults.Insert(0, templateFile.FullName);
            }

            foreach (var fileWithResults in filesWithResults)
            {
                // add analysis target if result not in root template
                ArtifactLocation analysisTarget = null;
                if (fileWithResults != templateFile.FullName)
                {
                    (var pathBelongsToRoot, var filePath) = GetFilePathInfo(templateFile.FullName);
                    analysisTarget = new ArtifactLocation
                    {
                        Uri = new Uri(
                            UriHelper.MakeValidUri(filePath),
                            UriKind.RelativeOrAbsolute),
                        UriBaseId = pathBelongsToRoot ? UriBaseIdString : null,
                    };
                }

                foreach ((var evaluation, var failedResults) in resultsByFile[fileWithResults])
                {
                    // get rule definition from first level evaluation
                    this.ExtractRule(evaluation);

                    // create location for each individual result
                    (var pathBelongsToRoot, var filePath) = GetFilePathInfo(fileWithResults);
                    var locations = failedResults.Select(result => new Location
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
                            Region = new Region { StartLine = result.SourceLocation.LineNumber },
                        },
                    }).ToList();

                    // Log result
                    SarifLogger.Log(this.rulesDictionary[evaluation.RuleId], new SarifResult
                    {
                        RuleId = evaluation.RuleId,
                        Level = GetLevelFromEvaluation(evaluation),
                        Message = new Message { Id = "default" }, // should be customized message for each result 
                        Locations = locations,
                        AnalysisTarget = analysisTarget,
                    });

                    totalResults++;
                }
            }

            filesAlreadyOutput.AddRange(filesWithResults);
        }

        internal string RootPath
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

        private (bool, string) GetFilePathInfo(string fileFullName)
        {
            bool isFileInRootPath = IsSubPath(this.RootPath, fileFullName);
            string filePath = isFileInRootPath ?
                Path.GetRelativePath(this.RootPath, fileFullName) :
                fileFullName;

            return (isFileInRootPath, filePath);
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
            this.SarifLogger?.Dispose();
            this.outputTextWriter?.Dispose();
            this.reportFileStream?.Dispose();

            Console.WriteLine($"{Environment.NewLine}Wrote {totalResults} {(totalResults == 1 ? "result" : "results")} to {reportFile.FullName}");
        }
    }
}
