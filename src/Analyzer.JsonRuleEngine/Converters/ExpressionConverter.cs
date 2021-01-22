// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Converters
{
    internal class ExpressionConverter : JsonConverter<ExpressionDefinition>
    {
        /// <summary>
        /// The property names that can be specified for LeafExpressions
        /// </summary>
        private static readonly HashSet<string> LeafExpressionJsonPropertyNames =
            typeof(LeafExpressionDefinition)
            .GetProperties(BindingFlags.Public| BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => (property.Name, Attribute: property.GetCustomAttribute<JsonPropertyAttribute>()))
            .Where(property => property.Attribute != null)
            .Select(property => property.Attribute.PropertyName ?? property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Parses an ExpressionDefinition from a JsonReader
        /// </summary>
        /// <param name="reader">The JsonReader</param>
        /// <param name="objectType">The type of the object</param>
        /// <param name="existingValue">The existing value of the object being read</param>
        /// <param name="hasExistingValue">Whether or not there is an existing value</param>
        /// <param name="serializer">The Json serializer</param>
        /// <returns></returns>
        public override ExpressionDefinition ReadJson(JsonReader reader, Type objectType, [AllowNull] ExpressionDefinition existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var jsonObject = JObject.Load(reader);

            var objectPropertyNames = jsonObject.Properties().Select(property => property.Name).ToList();

            // See if a property representing a structured expression is present.  If so, parse that structured expression.
            // TODO: Parse structured expressions

            // Verify an operator property exists, representing a LeafExpression
            var leafPropertyCount = objectPropertyNames.Count(property => LeafExpressionJsonPropertyNames.Contains(property));
            if (leafPropertyCount == 1)
            {
                return CreateExpressionDefinition<LeafExpressionDefinition>(jsonObject, serializer);
            }

            throw new JsonException(leafPropertyCount > 1 ?
                $"Too many expressions specified in evaluation.  Only one is allowed.  Original JSON: {jsonObject}" :
                $"Invalid evaluation in JSON.  No expressions are specified (must specify exactly one).  Original JSON: {jsonObject}");
        }

        /// <summary>
        /// Returns false.  This converter cannot write JSON.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Throws NotImplementedException.  This converter cannot write JSON.
        /// </summary>
        public override void WriteJson(JsonWriter writer, [AllowNull] ExpressionDefinition value, JsonSerializer serializer)
            => throw new NotImplementedException("This converter cannot write JSON.  This method should not be called.");

        /// <summary>
        /// Creates the requested ExpressionDefinion type from JSON.
        /// </summary>
        /// <typeparam name="T">The type of ExpressionDefinition</typeparam>
        /// <param name="jObject">The JSON object to parse from</param>
        /// <param name="serializer">The JSON serializer</param>
        /// <returns>An instance of the specified type parsed from the JSON</returns>
        private static T CreateExpressionDefinition<T>(JObject jObject, JsonSerializer serializer) where T: ExpressionDefinition, new()
        {
            // The object is created and populated explicitly here (instead of using serializer.Deserialize<>()).
            // Otherwise, this converter would continue to be called recusively without end.
            var expression = new T();
            serializer.Populate(jObject.CreateReader(), expression);
            return expression;
        }
    }
}
