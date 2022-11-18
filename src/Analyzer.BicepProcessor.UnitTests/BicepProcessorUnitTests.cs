// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.BicepProcessor;
using Microsoft.Azure.Templates.Analyzer.Core;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Analyzer.BicepProcessor.UnitTests
{
    [TestClass]
    public class BicepProcessorUnitTests
    {
        [TestMethod]
        public void ValidateBicepModuleMetadata()
        {
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "templates");
            string bicepPath = Path.Combine(directory, "TemplateWithMixedRefs.bicep");
            (_, var bicepMetadata) = BicepTemplateProcessor.ConvertBicepToJson(bicepPath);
            var actualModuleInfo = bicepMetadata.ModuleInfo.ToList();

            // Line numbers from Bicep.Core are 0-indexed (and maintained in module info)
            var jsonFullPath = Path.Combine(directory, "AppServicesLogs-Failures.json");
            var expectedModuleInfo = new List<SourceFileModuleInfo>()
            {
                new SourceFileModuleInfo("TemplateWithMixedRefs.bicep",
                    new Dictionary<int,string>() {
                        { 3, jsonFullPath },
                        { 11, "AppServicesLogs-Failures.bicep" },
                        { 19, "TemplateWithTwoArmRefs.bicep"},
                        { 27, "TemplateWithTwoBicepRefs.bicep"}
                    }),
                new SourceFileModuleInfo("TemplateWithTwoArmRefs.bicep",
                    new Dictionary<int,string>() {
                        { 3, jsonFullPath },
                        { 11, jsonFullPath },
                    }),
                new SourceFileModuleInfo("TemplateWithTwoBicepRefs.bicep",
                    new Dictionary<int,string>() {
                        { 3, "TemplateWithTwoArmRefs.bicep" },
                        { 11, "TemplateWithTwoArmRefs.bicep" },
                    })
            };

            Assert.AreEqual(expectedModuleInfo.Count, actualModuleInfo.Count);
            foreach (var expectedFileModuleInfo in expectedModuleInfo)
            {
                var actualFileModuleInfo = actualModuleInfo.FirstOrDefault(info => info.FileName == expectedFileModuleInfo.FileName);
                Assert.IsNotNull(actualFileModuleInfo);
                Assert.AreEqual(expectedFileModuleInfo.Modules.Count, actualFileModuleInfo.Modules.Count);
                Assert.IsFalse(expectedFileModuleInfo.Modules.Except(actualFileModuleInfo.Modules).Any());
            }
        }

        [TestMethod]
        public void ValidateLineNumbersForArmTemplateAsBicepModule()
        {
            var templateAnalyzer = TemplateAnalyzer.Create(false);
            string directory = Path.Combine(Directory.GetCurrentDirectory(), "templates");
            string armFilePath = Path.Combine(directory, "AppServicesLogs-Failures.json");
            string armAsModuleFilePath = Path.Combine(directory, "TemplateWithTwoArmRefs.bicep");
            string armTemplate = File.ReadAllText(armFilePath);

            var armEvaluations = templateAnalyzer.AnalyzeTemplate(armTemplate, armFilePath);
            var armAsModuleEvaluations = templateAnalyzer.AnalyzeTemplate(string.Empty, armAsModuleFilePath);

            // Verify only results in bicep file are from ARM module
            var sourceFilesWithResults = armAsModuleEvaluations
                .Select(eval => eval.GetFailedResults())
                .SelectMany(results => results)
                .Select(result => result.SourceLocation.FilePath)
                .Distinct();
            Assert.AreEqual(1, sourceFilesWithResults.Count());
            Assert.AreEqual(armFilePath, sourceFilesWithResults.First());

            var armFailingLines = GetFailingLineNumbers(armEvaluations);
            var armAsModuleFailingLines = GetFailingLineNumbers(armAsModuleEvaluations);
            Assert.IsTrue(Enumerable.SequenceEqual(armFailingLines, armAsModuleFailingLines));
        }

        private IList<int> GetFailingLineNumbers(IEnumerable<IEvaluation> evaluations)
        {
            HashSet<int> failedEvaluationLines = new();

            foreach (var evaluation in evaluations)
            {
                if (!evaluation.Passed)
                {
                    var failedLines = evaluation.GetFailedResults().Select(r => r.SourceLocation.LineNumber);
                    failedEvaluationLines.UnionWith(failedLines);
                }
            }

            var failingLines = failedEvaluationLines.ToList();
            failingLines.Sort();
            return failingLines;
        }
    }
}
