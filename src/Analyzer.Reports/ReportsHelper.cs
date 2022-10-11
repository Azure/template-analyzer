using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;
//using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// Helper class for ReportsWriters
    /// </summary>
    public class ReportsHelper
    {
        /// <summary>
        /// Get all distinct results in an evaluation bucketed by source location file
        /// </summary>
        /// <param name="evaluations">Evaluations to get results for</param>
        /// <param name="filesToSkip">Files to not include results from</param>
        /// <returns></returns>
        public static Dictionary<string, List<(IEvaluation, IList<Result>)>> GetResultsByFile(
            IEnumerable<Types.IEvaluation> evaluations,
            IEnumerable<string> filesToSkip)
        {
            return GetResultsByFile(evaluations, filesToSkip, out int _);
        }

        /// <summary>
        /// Get all distinct results in an evaluation bucketed by source location file
        /// </summary>
        /// <param name="evaluations">Evaluations to get results for</param>
        /// <param name="filesToSkip">Files to not include results from</param>
        /// <param name="passedEvaluations">Out parameter that gives number of passed evaluations</param>
        /// <returns></returns>
        public static Dictionary<string, List<(IEvaluation, IList<Result>)>> GetResultsByFile(
            IEnumerable<Types.IEvaluation> evaluations,
            IEnumerable<string> filesToSkip,
            out int passedEvaluations)
        {
            var resultsByFile = new Dictionary<string, List<(IEvaluation, IList<Result>)>>();
            passedEvaluations = 0;

            foreach (var evaluation in evaluations)
            {
                if (!evaluation.Passed)
                {
                    (var actualFile, var failedResults) = GetResultsByFileInternal(evaluation);

                    // a file's results may have already been output if analyzing directory
                    if (filesToSkip.Contains(actualFile))
                    {
                        continue;
                    }

                    if (!resultsByFile.ContainsKey(actualFile))
                    {
                        resultsByFile[actualFile] = new List<(IEvaluation, IList<Result>)>();
                    }

                    // skip any evaluations with duplicate results (i.e. two source locations from other templates refer to same result)
                    if (!resultsByFile[actualFile].Any(resultsTuple => evaluation.RuleId.Equals(resultsTuple.Item1.RuleId)
                        && Enumerable.SequenceEqual(resultsTuple.Item2, failedResults)))
                    {
                        resultsByFile[actualFile].Add((evaluation, failedResults));
                    }
                }
                else
                {
                    passedEvaluations++;
                }
            }

            return resultsByFile;
        }

        /// <summary>
        /// Gets all failed results in an evaluation, sorts, and returns rule ID
        /// </summary>
        /// <param name="evaluation">The evaluation to get results for</param>
        /// <returns>A list of distinct failed results</returns>
        public static (string, IList<Result>) GetResultsByFileInternal(Types.IEvaluation evaluation)
        {
            // get all distinct failed results in evaluation
            var failedResults = GetFailedResultsAsList(evaluation).Distinct().ToList();
            failedResults.Sort((x, y) => x.SourceLocation.LineNumber.CompareTo(y.SourceLocation.LineNumber));

            var actualFile = failedResults.First().SourceLocation.FilePath;

            return (actualFile, failedResults);
        }

        /// <summary>
        /// Gets all failed results in an evaluation and returns in a flattened list
        /// </summary>
        /// <param name="evaluation">The evaluation to get results from</param>
        /// <param name="failedResults">Accumulator used inrecursive calls</param>
        /// <returns>A list of failed results</returns>
        public static List<Result> GetFailedResultsAsList(Types.IEvaluation evaluation, List<Result> failedResults = null)
        {
            failedResults ??= new List<Result>();

            if (!evaluation.Result?.Passed ?? false)
            {
                failedResults.Add(evaluation.Result);
            }

            foreach (var eval in evaluation.Evaluations.Where(e => !e.Passed))
            {
                GetFailedResultsAsList(eval, failedResults);
            }

            return failedResults;
        }
    }
}
