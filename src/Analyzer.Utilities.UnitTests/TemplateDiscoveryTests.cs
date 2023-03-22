// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.Utilities.UnitTests
{
    [TestClass]
    public class TemplateDiscoveryTests
    {
        private static string executingDirectory = Directory.GetCurrentDirectory();
        private static string testTemplateDirectory = Path.Combine(executingDirectory[..executingDirectory.IndexOf(typeof(TemplateDiscoveryTests).Namespace)], "Analyzer.Cli.FunctionalTests", "Tests");

        [TestMethod]
        public void DiscoverTemplatesAndParametersInDirectory_DirectoryWithTemplatesAndParameters_ReturnsExpectedNumberOfFiles()
        {
            var discoveredTemplates = TemplateDiscovery.DiscoverTemplatesAndParametersInDirectory(new DirectoryInfo(testTemplateDirectory));

            Assert.AreEqual(18, discoveredTemplates.Count());

            // Create list of templates expecting a match with parameters
            var subDirectory = "ToTestSeparateParametersFile";
            var templateName = "TemplateWithSeparateParametersFile";

            var templatesWithParameters = new[] {
                Path.Combine(testTemplateDirectory, subDirectory, templateName + ".bicep"),
                Path.Combine(testTemplateDirectory, subDirectory, templateName + ".json") };

            var parametersToMatchToTemplates = new[] {
                Path.Combine(testTemplateDirectory, subDirectory, templateName + ".parameters.json"),
                Path.Combine(testTemplateDirectory, subDirectory, templateName + ".parameters-dev.json") };

            List<TemplateAndParams> pairsToAssertWereFound = new();

            foreach (var template in templatesWithParameters)
                foreach (var parameters in parametersToMatchToTemplates)
                    pairsToAssertWereFound.Add(new(new FileInfo(template), new FileInfo(parameters)));

            // Verify parameter files discovered with templates
            foreach (var pair in pairsToAssertWereFound)
            {
                Assert.IsNotNull(discoveredTemplates.Single(t => t.Template.FullName == pair.Template.FullName && t.Parameters.FullName == pair.Parameters.FullName),
                    $"Expected template {pair.Template.FullName} and parameters file {pair.Parameters.FullName} not discovered.");
            }
        }

        [DataTestMethod]
        [DataRow("AppServicesLogs-Passes.json", DisplayName = "ARM JSON template with no matching parameters")]
        [DataRow("AppServicesLogs-Passes.bicep", DisplayName = "Bicep template with no matching parameters")]
        [DataRow(@"ToTestSeparateParametersFile\TemplateWithSeparateParametersFile.json",
            "TemplateWithSeparateParametersFile.parameters.json",
            "TemplateWithSeparateParametersFile.parameters-dev.json",
            DisplayName = "ARM JSON template with 2 matching parameters files")]
        [DataRow(@"ToTestSeparateParametersFile\TemplateWithSeparateParametersFile.bicep",
            "TemplateWithSeparateParametersFile.parameters.json",
            "TemplateWithSeparateParametersFile.parameters-dev.json",
            DisplayName = "Bicep template with 2 matching parameters files")]
        public void FindParameterFilesForTemplate_ValidTemplateFile_ReturnsExpectedParametersFiles(string templateName, params string[] parameterFiles)
        {
            // Directory to test (if a sub-directory of `testTemplateDirectory`) is specified in the template name.
            // Grab that directory to target the rest of the test to it.
            var templateNameSplit = templateName.Split('\\');
            var directoryForTest = Path.Combine(templateNameSplit[..^1].Prepend(testTemplateDirectory).ToArray());

            // Get the full template path from the directory above.
            var template = Path.Combine(directoryForTest, templateNameSplit[^1]);
            var pairs = TemplateDiscovery.FindParameterFilesForTemplate(new FileInfo(template)).ToList();

            if (parameterFiles.Length == 0)
            {
                // No expected parameters files
                Assert.AreEqual(1, pairs.Count);
                Assert.IsNull(pairs[0].Parameters, $"Unexpected parameters file found: template '{pairs[0].Template.FullName}'; parameters '{pairs[0].Parameters?.FullName}'");
            }
            else
            {
                // Expect to find a pair for each expected parameters file
                Assert.AreEqual(parameterFiles.Length, pairs.Count);
                Assert.IsFalse(pairs.Any(p => p.Parameters == null), "A template was returned without a matching parameters file");

                foreach (var expectedParams in parameterFiles)
                {
                    Assert.IsNotNull(pairs.Single(p => p.Parameters.FullName == Path.Combine(directoryForTest, expectedParams)), $"Expected parameters file not found: {expectedParams}");
                }
            }
        }
    }
}