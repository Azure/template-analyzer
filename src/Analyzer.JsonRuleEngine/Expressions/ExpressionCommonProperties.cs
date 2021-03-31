using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions
{
    /// <summary>
    /// Properties common across all <see cref="Expression"/> types.
    /// </summary>
    internal class ExpressionCommonProperties
    {
        /// <summary>
        /// Gets or sets the resource type specified for the <see cref="Expression"/>.
        /// </summary>
        public string ResourceType { get; set; }

        /// <summary>
        /// Gets or sets the path specified for the <see cref="Expression"/>.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the where property specified for the <see cref="Expression"/>.
        /// </summary>
        public Expression Where { get; set; }
    }
}
