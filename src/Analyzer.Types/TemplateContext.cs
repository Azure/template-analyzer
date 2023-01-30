// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.Types
{
    /// <summary>
    /// Contains all forms of an ARM template and information about the template
    /// </summary>
    public class TemplateContext
    {
        /// <summary>
        /// The original template parsed into JSON
        /// </summary>
        public JToken OriginalTemplate { get; set; }

        /// <summary>
        /// The template with resolved parameter/variable references and expressions
        /// </summary>
        public JToken ExpandedTemplate { get; set; }
        
        /// <summary>
        /// Whether this template was originally a Bicep file
        /// </summary>
        public bool IsBicep { get; set; }

        /// <summary>
        /// Includes metadata from Bicep compilation (source map and module info)
        /// </summary>
        public object BicepMetadata { get; set; }

        /// <summary>
        /// A unique identifier for the template
        /// </summary>
        public string TemplateIdentifier { get; set; }

        /// <summary>
        /// Whether this template is the top-level template in a deployment or nested within another template
        /// </summary>
        public bool IsMainTemplate { get; set; } = true;

        /// <summary>
        /// Mapping between resources in the expanded template to their original resource in 
        /// the original template. Used to get line numbers.
        /// The key is the path in the expanded template with value being the path
        /// in the original template.
        /// </summary>
        public Dictionary<string, string> ResourceMappings { get; set; }

        /// <summary>
        /// Prefix of path to nested template properties/resources
        /// </summary>
        public string PathPrefix { get; set; }

        /// <summary>
        /// Template context for the immediate parent template
        /// </summary>
        public TemplateContext ParentContext { get; set; }
    }
}