using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// 
    /// </summary>
    public class ReportsHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluations"></param>
        /// <param name="filesToSkip"></param>
        /// <returns></returns>
        public static Dictionary<string, List<(IEvaluation, int[])>> GetResultsByFile(
            IEnumerable<Types.IEvaluation> evaluations,
            IEnumerable<string> filesToSkip)
        {
            return GetResultsByFile(evaluations, filesToSkip, out int _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluations"></param>
        /// <param name="filesToSkip"></param>
        /// <param name="passedEvaluations"></param>
        /// <returns></returns>
        public static Dictionary<string, List<(IEvaluation, int[])>> GetResultsByFile(
            IEnumerable<Types.IEvaluation> evaluations,
            IEnumerable<string> filesToSkip,
            out int passedEvaluations)
        {
            var resultsByFile = new Dictionary<string, List<(IEvaluation, int[])>>();
            passedEvaluations = 0;


            foreach (var evaluation in evaluations)
            {
                if (evaluation.RuleId != "TA-000003") continue; // DEbug

                if (!evaluation.Passed)
                {
                    (var actualFile, var failedLines) = GetEvaluationResultInfo(evaluation);

                    // a file's results may have already been output if analyzing directory
                    if (filesToSkip.Contains(actualFile))
                    {
                        continue;
                    }

                    if (!resultsByFile.ContainsKey(actualFile))
                    {
                        resultsByFile[actualFile] = new List<(IEvaluation, int[])>();
                    }

                    if (!resultsByFile[actualFile].Select(i => i.Item2).Contains(failedLines))
                    {
                        resultsByFile[actualFile].Add((evaluation, failedLines));
                    }
                }
                else
                {
                    passedEvaluations++;
                }
            }

            foreach (var file in resultsByFile.Keys)
            {
                resultsByFile[file] = resultsByFile[file].Distinct().ToList();
            }

            return resultsByFile;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluation"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static (string, int[]) GetEvaluationResultInfo(Types.IEvaluation evaluation)
        {
            // get all distinct failed results in evaluation
            var failedResults = GetFailedResults(evaluation).Distinct().ToList();

            // assumption: all results in a top-level evaluation are in a single resource and therefore in a single source file, so we can just look at the first to get them all
            // TODO: validating assumption
            var failedActualFiles = failedResults.Select(r => r.SourceLocation.GetActualLocation().FilePath).Distinct().ToList();
            if (failedActualFiles.Count != 1) throw new Exception("not 1 actual source file in top level eval");

            var actualFile = failedResults.First().SourceLocation.GetActualLocation().FilePath;
            var failedLines = failedResults.Select(r => r.SourceLocation.GetActualLocation().LineNumber).ToArray();

            return (actualFile, failedLines);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluation"></param>
        /// <param name="failedResults"></param>
        /// <returns></returns>
        public static List<IResult> GetFailedResults(Types.IEvaluation evaluation, List<IResult> failedResults = null)
        {
            failedResults ??= new List<IResult>();

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
