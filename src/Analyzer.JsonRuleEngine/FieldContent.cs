// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine
{
    /// <summary>
    /// The content and properties of a given JToken field.
    /// </summary>
    public class FieldContent
    {
        /// <summary>
        /// The Value of the JToken field.
        /// </summary>
        public JToken Value { get; set; }
        // Other properties about a field can go here
        // Things like isSecret, isId, etc.
        // These properties can be set in the constructor and is FieldValue's job to determine
        // if a field is a secret, id, etc.

        /// <summary>
        /// Operator to implicitly create a <c>FieldContent</c> from a JToken.
        /// </summary>
        /// <param name="value">The JToken to use as the field.</param>
        public static implicit operator FieldContent(JToken value) => new FieldContent { Value = value };
    }
}