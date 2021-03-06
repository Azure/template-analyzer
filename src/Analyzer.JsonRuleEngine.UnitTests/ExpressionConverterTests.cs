﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Converters;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class ExpressionConverterTests
    {
        // Dictionary of property names to PropertyInfo
        private static readonly Dictionary<string, PropertyInfo> leafExpressionJsonProperties =
            typeof(LeafExpressionDefinition)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(property => (Property: property, Attribute: property.GetCustomAttribute<JsonPropertyAttribute>()))
            .Where(property => property.Attribute != null)
            .ToDictionary(property => property.Attribute.PropertyName ?? property.Property.Name, property => property.Property, StringComparer.OrdinalIgnoreCase);

        [DataTestMethod]
        [DataRow("hasValue", true, DisplayName = "{\"HasValue\": true}")]
        [DataRow("exists", false, DisplayName = "{\"Exists\": false}")]
        [DataRow("equals", "someString", DisplayName = "{\"Equals\": \"someString\"}")]
        [DataRow("notEquals", 0, DisplayName = "{\"NotEquals\": 0}")]
        public void ReadJson_LeafWithValidOperator_ReturnsCorrectTypeAndValues(string operatorProperty, object operatorValue)
        {
            var @object = ReadJson(string.Format(@"
                {{
                    ""resourceType"": ""someResource/resourceType"",
                    ""path"": ""some.json.path"",
                    ""{0}"": {1}
                }}",
                operatorProperty,
                JsonConvert.SerializeObject(operatorValue)));

            Assert.AreEqual(typeof(LeafExpressionDefinition), @object.GetType());

            var expression = @object as LeafExpressionDefinition;
            Assert.AreEqual("some.json.path", expression.Path);
            Assert.AreEqual("someResource/resourceType", expression.ResourceType);

            // Iterate through possible expressions and ensure only the specified one has a value
            foreach (var expressionProperty in leafExpressionJsonProperties)
            {
                var parsedValue = expressionProperty.Value.GetValue(expression);
                if (expressionProperty.Key.Equals(operatorProperty, StringComparison.OrdinalIgnoreCase))
                {
                    if (expressionProperty.Value.PropertyType == typeof(bool?))
                    {
                        Assert.AreEqual(operatorValue, (bool)parsedValue);
                    }
                    else if (expressionProperty.Value.PropertyType == typeof(string))
                    {
                        Assert.AreEqual(operatorValue, (string)parsedValue);
                    }
                    else
                    {
                        Assert.AreEqual(new JValue(operatorValue), parsedValue);
                    }
                }
                else
                {
                    Assert.IsNull(parsedValue);
                }
            }
        }

        [TestMethod]
        public void ReadJson_AllOfWithValidExpressions_ReturnsCorrectTypeAndValues()
        {
            var @object = ReadJson(@"
                {
                    ""resourceType"": ""someResource/resourceType"",
                    ""path"": ""some.json.path"",
                    ""allOf"": [ 
                        { 
                            ""path"": ""some.other.path"", 
                            ""hasValue"": true 
                        }, 
                        { 
                            ""path"": ""some.other.path"", 
                            ""equals"": true 
                        } 
                    ]
                }");

            Assert.AreEqual(typeof(AllOfExpressionDefinition), @object.GetType());

            var expression = @object as AllOfExpressionDefinition;
            Assert.AreEqual("some.json.path", expression.Path);
            Assert.AreEqual("someResource/resourceType", expression.ResourceType);
        }

        [DataTestMethod]
        [DataRow("hasValue", "string", DisplayName = "\"HasValue\": \"string\"")]
        [DataRow("exists", new int[0], DisplayName = "\"Exists\": []")]
        [ExpectedException(typeof(JsonReaderException))]
        public void ReadJson_LeafWithInvalidOperator_ThrowsParsingException(string operatorProperty, object operatorValue)
        {
            ReadJson(string.Format(@"
                {{
                    ""resourceType"": ""resourceType"",
                    ""path"": ""path"",
                    ""{0}"": {1}
                }}",
                operatorProperty,
                JsonConvert.SerializeObject(operatorValue)));
        }

        [DataTestMethod]
        [DataRow("allOf", "string", DisplayName = "\"AllOf\": \"string\"")]
        [DynamicData(nameof(EmptyAllOfArray), DynamicDataSourceType.Method, DynamicDataDisplayName = "GetAllOfIsEmptyDynamicDataDisplayName")]
        [ExpectedException(typeof(JsonException))]
        public void ReadJson_StructuredExpressionWithInvalidExpression_ThrowsParsingException(string operatorProperty, object operatorValue)
        {
            ReadJson(string.Format(@"
                {{
                    ""resourceType"": ""resourceType"",
                    ""path"": ""path"",
                    ""{0}"": {1}
                }}",
                operatorProperty,
                JsonConvert.SerializeObject(operatorValue)));
        }

        [TestMethod]
        [DataRow(DisplayName = "No operators")]
        [DataRow("hasValue", true, "exists", true, DisplayName = "HasValue and Exists")]
        [ExpectedException(typeof(JsonException))]
        public void ReadJson_LeafWithInvalidOperatorCount_ThrowsParsingException(params object[] operators)
        {
            var leafDefinition = "{\"resourceType\": \"resource\", \"path\": \"path\"";

            if (operators.Length % 2 != 0)
            {
                Assert.Fail("Must provide an operator value for each operator property.");
            }

            int index = 0;
            foreach (var op in operators)
            {
                if (index++ % 2 == 0)
                {
                    if (!(op is string))
                    {
                        Assert.Fail("Operator property (first of each pair) must be a string");
                    }
                    leafDefinition += $", \"{op}\": ";
                }
                else
                {
                    var jsonValue = JsonConvert.SerializeObject(op);
                    leafDefinition += jsonValue;
                }
            }

            leafDefinition += "}";

            try
            {
                ReadJson(leafDefinition);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.IndexOf(operators.Length > 0 ? "too many" : "invalid", StringComparison.OrdinalIgnoreCase) >= 0);
                throw;
            }
        }

        [TestMethod]
        public void ReadJson_NullTokenType_ReturnsNull()
        {
            var nullTokenReader = JObject.Parse("{\"Key\": null}").CreateReader();

            nullTokenReader.Read(); // Read start of object
            nullTokenReader.Read(); // Read Key
            nullTokenReader.Read(); // Read value (null)

            Assert.IsNull(
                new ExpressionConverter().ReadJson(
                    nullTokenReader,
                    typeof(ExpressionDefinition),
                    null,
                    JsonSerializer.CreateDefault()));
        }

        [TestMethod]
        public void CanRead_ReturnsTrue()
        {
            Assert.IsTrue(new ExpressionConverter().CanRead);
        }

        [TestMethod]
        public void CanWrite_ReturnsFalse()
        {
            Assert.IsFalse(new ExpressionConverter().CanWrite);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void WriteJson_ThrowsException()
        {
            new ExpressionConverter().WriteJson(null, null, null);
        }

        private static object ReadJson(string jsonString)
            => new ExpressionConverter().ReadJson(
                JObject.Parse(jsonString).CreateReader(),
                typeof(ExpressionDefinition),
                null,
                JsonSerializer.CreateDefault());

        static IEnumerable<object[]> EmptyAllOfArray()
        {
            yield return new object[] { "allOf", new object[0] };
        }

        public static string GetAllOfIsEmptyDynamicDataDisplayName(MethodInfo methodInfo, object[] data)
        {
            return "\"AllOf\": []";
        }
    }
}