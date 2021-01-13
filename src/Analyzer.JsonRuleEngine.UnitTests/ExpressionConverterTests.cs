// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Azure.Templates.Analyzer.JsonRuleEngine.UnitTests
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

        [DataTestMethod]
        [DataRow("hasValue", "string", DisplayName = "HasValue: \"string\"")]
        [DataRow("exists", new int[0], DisplayName = "Exists: []")]
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

        [TestMethod]
        [ExpectedException(typeof(JsonSerializationException))]
        public void ReadJson_LeafWithoutPath_ThrowsParsingException()
        {
            ReadJson("{ \"resourceType\": \"resourceType\", \"hasValue\": true }");
        }

        [TestMethod]
        [ExpectedException(typeof(JsonSerializationException))]
        public void ReadJson_LeafWithNullPath_ThrowsParsingException()
        {
            ReadJson("{ \"resourceType\": \"resourceType\", \"path\": null, \"hasValue\": true }");
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
    }
}