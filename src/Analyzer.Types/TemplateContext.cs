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
        /// A unique identifier for the template
        /// </summary>
        public string TemplateIdentifier { get; set; }

        /// <summary>
        /// Whether this template is the top-level template in a deployment or nested within another template
        /// </summary>
        public bool IsMainTemplate { get; set; }

        /// <summary>
        /// Mapping between resources in the expanded template to their original resource in 
        /// the original template. Used to get line numbers.
        /// The key is the path in the expanded template with value being the path
        /// in the original template.
        /// </summary>
        public Dictionary<string, string> ResourceMappings { get; set; }

        /// <summary>
        /// Value by which line numbers in the nested template are offset from the parent template
        /// </summary>
        public int Offset { get; set; }
    }
}