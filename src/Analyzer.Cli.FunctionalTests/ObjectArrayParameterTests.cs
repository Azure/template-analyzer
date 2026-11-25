// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Templates.Analyzer.Cli;
using Microsoft.Azure.Templates.Analyzer.Core;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Analyzer.Cli.FunctionalTests
{
    [TestClass]
    public class ObjectArrayParameterTests
    {
        private static string GetFixturePath(string fileName) =>
            Path.Combine(Directory.GetCurrentDirectory(), "Tests", "ObjectArrayParameter", fileName);

        [TestMethod]
        public async Task AnalyzeTemplate_ObjectArrayWithoutDefault_ReportsLiteralFindingAtOriginalLine()
        {
            var templatePath = GetFixturePath("array.json");
            var rulesPath = GetFixturePath("rules.json");
            var analyzer = TemplateAnalyzer.Create(false, false, File.ReadAllText(rulesPath));
            var evaluation = analyzer.AnalyzeTemplate(File.ReadAllText(templatePath), templatePath).Single();
            var failure = evaluation.GetFailedResults().Single();

            Assert.AreEqual("TA-REPRO-001", evaluation.RuleId);
            Assert.AreEqual("resources[0].apiVersion", failure.JsonPath);
            Assert.AreEqual(7, failure.SourceLocation.LineNumber);
            Assert.AreEqual(templatePath, failure.SourceLocation.FilePath);

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"ObjectArray-{Guid.NewGuid()}.sarif");
            try
            {
                var exitCode = await new CommandLineParser().InvokeCommandLineAPIAsync(new[]
                {
                    "analyze-template", templatePath, "--custom-json-rules-path", rulesPath,
                    "--report-format", "Sarif", "-o", outputPath
                });

                Assert.AreEqual((int)ExitCode.Violation, exitCode);
                var run = JObject.Parse(File.ReadAllText(outputPath))["runs"][0];
                var result = run["results"].Single(result => (string)result["ruleId"] == "TA-REPRO-001");
                Assert.AreEqual(7, (int)result["locations"][0]["physicalLocation"]["region"]["startLine"]);
                Assert.IsTrue(run["invocations"][0]["toolExecutionNotifications"]
                    .Any(notification => ((string)notification["message"]["text"]).Contains("could not be resolved")));
                Assert.IsFalse(run["invocations"][0]["toolExecutionNotifications"]?
                    .Any(notification => (string)notification["level"] == "error") ?? false);
            }
            finally
            {
                File.Delete(outputPath);
            }
        }

        [TestMethod]
        public async Task AnalyzeTemplate_DuplicateResources_ReportsProcessingError()
        {
            var template = JObject.Parse(File.ReadAllText(GetFixturePath("array.json")));
            template["resources"] = new JArray(template["resources"][0].DeepClone(), template["resources"][0].DeepClone());
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), $"DuplicateResources-{Guid.NewGuid()}.json");
            var outputPath = Path.ChangeExtension(templatePath, ".sarif");
            try
            {
                File.WriteAllText(templatePath, template.ToString());
                var exitCode = await new CommandLineParser().InvokeCommandLineAPIAsync(new[]
                {
                    "analyze-template", templatePath,
                    "--custom-json-rules-path", GetFixturePath("rules.json"),
                    "--report-format", "Sarif", "-o", outputPath
                });

                Assert.AreEqual((int)ExitCode.ErrorAnalysis, exitCode);
                var run = JObject.Parse(File.ReadAllText(outputPath))["runs"][0];
                Assert.IsTrue(run["invocations"][0]["toolExecutionNotifications"]
                    .Any(notification => (string)notification["level"] == "error" && notification["exception"] != null));
            }
            finally
            {
                File.Delete(templatePath);
                File.Delete(outputPath);
            }
        }
    }
}
