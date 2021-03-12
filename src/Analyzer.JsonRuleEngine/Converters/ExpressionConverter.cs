// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Utilities;
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
        /// The property names that can be specified for AllOfExpressions
        /// </summary>
        private static readonly HashSet<string> AllOfExpressionJsonPropertyNames =
            typeof(AllOfExpressionDefinition)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => (property.Name, Attribute: property.GetCustomAttribute<JsonPropertyAttribute>()))
            .Where(property => property.Attribute != null)
            .Select(property => property.Attribute.PropertyName ?? property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The property names that can be specified for AnyOfExpressions
        /// </summary>
        private static readonly HashSet<string> AnyOfExpressionJsonPropertyNames =
            typeof(AnyOfExpressionDefinition)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
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

            var expressionJsonPropertyNames = GetExpressionJsonPropertyNames();
            var structuredExpressions = GetStructuredExpressionJsonPropertyNames();

            ValidateExpressions(jsonObject, structuredExpressions, expressionJsonPropertyNames);

            // See if a property representing a structured expression is present.  If so, parse that structured expression.
            var structuredExpression = objectPropertyNames.FirstOrDefault(property => structuredExpressions.Contains(property));
            if (!string.IsNullOrEmpty(structuredExpression))
            {
                if (objectPropertyNames.Contains("allOf", StringComparer.OrdinalIgnoreCase))
                {
                    return CreateExpressionDefinition<AllOfExpressionDefinition>(jsonObject, serializer);
                }
                else if (objectPropertyNames.Contains("anyOf", StringComparer.OrdinalIgnoreCase))
                {
                    return CreateExpressionDefinition<AnyOfExpressionDefinition>(jsonObject, serializer);
                }

                throw new JsonException($"Expression is not supported. One of the following fields is not supported: {string.Join(", ", objectPropertyNames.ToArray())}");
            }
            else
            {
                return CreateExpressionDefinition<LeafExpressionDefinition>(jsonObject, serializer);
            }
        }

        internal HashSet<string> GetExpressionJsonPropertyNames()
        {
            var structuredExpressions = GetStructuredExpressionJsonPropertyNames();

            var expressionJsonPropertyNames = LeafExpressionJsonPropertyNames;
            expressionJsonPropertyNames.UnionWith(structuredExpressions);

            return expressionJsonPropertyNames;
        }

        internal HashSet<string> GetStructuredExpressionJsonPropertyNames()
        {
            // Add new structuredExpressions here
            var structuredExpressions = AllOfExpressionJsonPropertyNames;
            structuredExpressions.UnionWith(AnyOfExpressionJsonPropertyNames);

            return structuredExpressions;
        }

        private void ValidateExpressions(JObject jsonObject, HashSet<string> structuredExpressionJsonPropertyNames, HashSet<string> expressionJsonPropertyNames)
        {
            var objectPropertyNames = jsonObject.Properties().Select(property => property.Name).ToList();

            // Verify an operator property exists, representing an Expression
            var expressionPropertyCount = objectPropertyNames.Count(property => expressionJsonPropertyNames.Contains(property));
            if (expressionPropertyCount == 1)
            {
                var structuredExpressionJsonPropertyName = objectPropertyNames.FirstOrDefault(property => structuredExpressionJsonPropertyNames.Contains(property));
                if (!string.IsNullOrEmpty(structuredExpressionJsonPropertyName))
                {
                    // If there are no leaf expressions defined in 
                    // the structured expression, throw an error
                    var structuredExpression = jsonObject.InsensitiveToken(structuredExpressionJsonPropertyName);

                    if (structuredExpression.Count() == 0)
                    {
                        throw new JsonException($"No leaf expressions were specified in the {structuredExpressionJsonPropertyName} expression");
                    }
                }

                return;
            }

            throw new JsonException(expressionPropertyCount > 1 ?
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
