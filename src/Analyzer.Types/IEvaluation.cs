// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Describes the evaluation of a TemplateAnalyzer rule against an ARM template
    /// </summary>
    public interface IEvaluation
    {
        /// <summary>
        /// Gets the id of the rule this evaluation is for.
        /// </summary>
        public string RuleId { get; }

        /// <summary>
        /// Gets the description of the rule this evaluation is for.
        /// </summary>
        public string RuleDescription { get; }

        /// <summary>
        /// Gets the recommendation for addressing this evaluation.
        /// </summary>
        public string Recommendation { get; }

        /// <summary>
        /// Gets the Uri where help for this evaluation can be found.
        /// </summary>
        public string HelpUri { get; }

        /// <summary>
        /// Gets the Severity of the rule this evaluation is for.
        /// </summary>
        public Severity Severity { get; }

        /// <summary>
        /// Gets the identifier of the file this evaluation is for.
        /// </summary>
        public string FileIdentifier { get; }

        /// <summary>
        /// Gets a value indicating whether or not the rule for this evaluation passed.
        /// </summary>
        public bool Passed { get; }

        /// <summary>
        /// Gets the collections of evaluations from this evaluation.
        /// </summary>
        public IEnumerable<IEvaluation> Evaluations { get; }

        /// <summary>
        /// Gets the collections of results from this evaluation.
        /// </summary>
        public Result Result { get; }

        /// <summary>
        /// Gets whether this evaluation has corresponding results.
        /// </summary>
        /// <returns>Whether this evaluation has corresponding results</returns>
        public bool HasResults { get; }
    }

    /// <summary>
    /// Extension methods for IEvaluation interface
    /// </summary>
    public static class IEvaluationExtensions
    {
        /// <summary>
        /// Gets all failed results in an evaluation and returns in a flattened list
        /// </summary>
        /// <param name="evaluation">The evaluation to get results from</param>
        /// <param name="failedResults">Accumulator used inrecursive calls</param>
        /// <returns>A list of failed results</returns>
        public static List<Result> GetFailedResults(this Types.IEvaluation evaluation, List<Result> failedResults = null)
        {
            failedResults ??= new List<Result>();

            if (!evaluation.Result?.Passed ?? false)
            {
                failedResults.Add(evaluation.Result);
            }

            foreach (var eval in evaluation.Evaluations.Where(e => !e.Passed))
            {
                GetFailedResults(eval, failedResults);
            }

            return failedResults;
        }
    }
}