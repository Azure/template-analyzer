// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Utilities
{
    /// <summary>
    /// An <c>ILineNumberResolver</c> used for resolving line numbers from a processed JSON template to the original JSON template
    /// </summary>
    public class JsonLineNumberResolver : IJsonLineNumberResolver
    {
        private static readonly Regex resourceIndexInPath = new Regex(@"Resources\[(?<index>\d+)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Given a JSON path in a processed JSON template, find the equivalent line number
        /// in the original JSON template.
        /// </summary>
        /// <param name="pathInProcessedTemplate">The path in the processed template
        /// to find the line number of in the original template.</param>
        /// <param name="processedTemplateRoot">The root of the processed template.</param>
        /// <param name="originalTemplateRoot">The root of the original template.</param>
        /// <returns>The line number of the equivalent location in the original template,
        /// or 0 if it can't be determined.</returns>
        public int ResolveLineNumberForOriginalTemplate(
            string pathInProcessedTemplate,
            JToken processedTemplateRoot,
            JToken originalTemplateRoot)
        {
            /*
             * 4 Known scenarios:
             * 
             * 1. The path in the processed template exactly matches a path in the original template.
             *     - This path can be used as-is to identify the line number in the original template.
             *     Example:
             *       processed: resources[2].properties.enabled
             *       original : resources[2].properties.enabled
             *       used path: resources[2].properties.enabled
             * 
             * 2. The path in the processed template partially matches a path in the original template,
             *    but extends beyond a valid path, and the first missing property is not a resources[] array.
             *     - The rule was looking for a property that isn't defined in the original template.
             *       Similar to scenario 1, the line number where the path ends in the original template
             *       can be taken directly.
             *     Example:
             *       processed: parameters.numberOfCopies.minValue
             *       original : parameters.numberOfCopies
             *       used path: parameters.numberOfCopies
             * 
             * 3. The path in the processed template begins with the resources[] array and can't match
             *    a valid resource in the original template (i.e. the index is too large).
             *     - There must have been copies of resources added to the processed template from
             *       a copy loop in a resource.  Get the name of the copy loop, find
             *       the resource in the original template defining that name, and replace the resources[] index
             *       in the processed template path with the index of the resource that defines the copy.
             *     Example:
             *       processed: resources[9].properties.configuration.encryption
             *       original : -
             *       used path: resources[3].properties.configuration.encryption (if resource 9 is a copy of resource 3)
             * 
             * 4. The path in the processed template partially matches a path in the original template,
             *    but extends beyond a valid path, and the first missing property is a resources[] array
             *    that is within a higher-level resources[] array.
             *     - This must be a copied resource from post-processing in the TemplateProcessor library.
             *       The source resource must be identified in the original template, and the path to that
             *       resource will replace the path that led to the copied resource.  Then, try matching the
             *       path again.  (Fortunately, JSON templates do not allow copy loops in child resources.
             *       https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/copy-resources#iteration-for-a-child-resource)
             *     Example:
             *       processed: resources[5].resources[0].properties.debug
             *       original : resources[5]
             *       used path: resources[7].properties.debug (if resource 7 was copied into resource 5 as a child in pre-processing)
             */

            if (pathInProcessedTemplate == null || originalTemplateRoot == null)
            {
                throw new ArgumentNullException(pathInProcessedTemplate == null
                    ? nameof(pathInProcessedTemplate)
                    : nameof(originalTemplateRoot));
            }

            var tokenFromOriginalTemplate = originalTemplateRoot.InsensitiveToken(pathInProcessedTemplate, InsensitivePathNotFoundBehavior.LastValid);

            int? CheckForScenarios1and2()
            {
                var originalTemplatePath = tokenFromOriginalTemplate.Path;
                var unmatchedPathInOriginalTemplate = pathInProcessedTemplate.Substring(startIndex: originalTemplatePath.Length);

                if (originalTemplatePath.Length == pathInProcessedTemplate.Length // Scenario 1
                    || !unmatchedPathInOriginalTemplate.StartsWith("resources", StringComparison.OrdinalIgnoreCase)) // Scenario 2
                {
                    return (tokenFromOriginalTemplate as IJsonLineInfo)?.LineNumber ?? 0;
                }
                return null;
            }

            var scenario1Or2LineNumber = CheckForScenarios1and2();
            if (scenario1Or2LineNumber != null)
            {
                return scenario1Or2LineNumber.Value;
            }

            // At this point, this could be Scenario 3 and/or Scenario 4 (both could be true).
            // First see if it's scenario 3.  If it is, update the path and get the new JToken,
            // then continue to see if Scenario 4 has happened also.

            // The JToken returned from looking up the parsed template path is just
            // pointing to the top level of the entire original template, meaning
            // no part of path can be found in the original template. This is Scenario 3.
            if (tokenFromOriginalTemplate.Equals(originalTemplateRoot))
            {
                var match = resourceIndexInPath.Match(pathInProcessedTemplate);
                if (match.Success)
                {
                    // Verify the processed template is available.
                    // (Avoid throwing earlier since this is not always needed.)
                    if (processedTemplateRoot == null)
                    {
                        throw new ArgumentNullException(nameof(processedTemplateRoot));
                    }

                    // See if the requested resource is a copied resource
                    var resourceWithIndex = match.Value;
                    var parsedResource = processedTemplateRoot.InsensitiveToken($"{resourceWithIndex}.copy.name", InsensitivePathNotFoundBehavior.Null);
                    if (parsedResource != null)
                    {
                        // Find resource with this copy name in the original template
                        var copyName = parsedResource.Value<string>();
                        foreach (var resource in originalTemplateRoot.InsensitiveToken("resources").Children())
                        {
                            var thisResourceCopyName = resource.InsensitiveToken("copy.name")?.Value<string>();
                            if (string.Equals(copyName, thisResourceCopyName))
                            {
                                // This is the original resource for the expanded copy.
                                // Use this resource's index and update the path.
                                pathInProcessedTemplate = resourceIndexInPath.Replace(pathInProcessedTemplate, resource.Path);
                                tokenFromOriginalTemplate = originalTemplateRoot.InsensitiveToken(pathInProcessedTemplate, InsensitivePathNotFoundBehavior.LastValid);

                                // At this point, Scenario 4 could still also be possible.  Check for Scenarios 1 and 2,
                                // and continue to Scenario 4 if those aren't the case.
                                scenario1Or2LineNumber = CheckForScenarios1and2();
                                if (scenario1Or2LineNumber != null)
                                {
                                    return scenario1Or2LineNumber.Value;
                                }
                            }
                        }

                        // Unknown issue - why is there an extra resource without an original?
                        return 0;
                    }
                    else
                    {
                        // Unknown issue - why is there an extra resource without a copy property?
                        return 0;
                    }
                }
                else
                {
                    // Unknown issue - where did a new top-level property come from?
                    return 0;
                }
            }

            // Scenario 4
            // TODO
            return 0;
        }

        private static Exception ArgumentNullException(string v)
        {
            throw new NotImplementedException();
        }
    }
}
