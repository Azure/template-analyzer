// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json.Linq;

namespace Armory.JsonRuleEngine
{
    /// <summary>
    /// The content and properties of a given JToken field
    /// </summary>
    public class FieldContent
    {
        /// <summary>
        /// The Value of the JToken field
        /// </summary>
        public JToken Value { get; set; }
        // Other properties about a field can go here
        // Things like isSecret, isId, etc.
        // These properties can be set in the constructor and is FieldValue's job to determine
        // if a field is a secret, id, etc.

    }
}