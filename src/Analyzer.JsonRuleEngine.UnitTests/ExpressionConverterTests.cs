// Copyright (c) Microsoft Corporation.
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
            .Where(property => !property.GetMethod.IsVirtual)
            .Select(property => (Property: property, Attribute: property.GetCustomAttribute<JsonPropertyAttribute>()))
            .Where(property => property.Attribute != null)
            .ToDictionary(property => property.Attribute.PropertyName ?? property.Property.Name, property => property.Property, StringComparer.OrdinalIgnoreCase);

        [DataTestMethod]
        [DataRow("hasValue", true, DisplayName = "{\"HasValue\": true}")]
        [DataRow("hasValue", false, "hasValue", true, DisplayName = "{\"HasValue\": false}, Where: {\"HasValue\": true}")]
        [DataRow("exists", false, DisplayName = "{\"Exists\": false}")]
        [DataRow("equals", "someString", DisplayName = "{\"Equals\": \"someString\"}")]
        [DataRow("equals", "someString", "exists", true, DisplayName = "{\"Equals\": \"someString\"}, Where: {\"Exists\": true}")]
        [DataRow("notEquals", 0, DisplayName = "{\"NotEquals\": 0}")]
        public void ReadJson_LeafWithValidOperator_ReturnsCorrectTypeAndValues(string operatorProperty, object operatorValue, params object[] whereCondition)
        {
            // If whereCondition is populated, add a Where condition into JSON to parse.
            string optionalWhereBlock = string.Empty;
            if (whereCondition.Length > 0)
            {
                if (whereCondition.Length != 2)
                    Assert.Fail($"{nameof(whereCondition)} must contain (only) the operator and value.");

                optionalWhereBlock = $@"
                    ""where"": {{
                        ""resourceType"": ""whereResource/resourceType"",
                        ""path"": ""some.json.path.where"",
                        ""{whereCondition[0]}"": {JsonConvert.SerializeObject(whereCondition[1])}
                    }},";
            }

            var @object = ReadJson($@"
                {{
                    ""resourceType"": ""someResource/resourceType"",
                    ""path"": ""some.json.path"",
                    {optionalWhereBlock}
                    ""{operatorProperty}"": {JsonConvert.SerializeObject(operatorValue)}
                }}");

            Assert.AreEqual(typeof(LeafExpressionDefinition), @object.GetType());

            var expression = @object as LeafExpressionDefinition;
            Assert.AreEqual("some.json.path", expression.Path);
            Assert.AreEqual("someResource/resourceType", expression.ResourceType);

            void ValidateLeafExpression(LeafExpressionDefinition leaf, string property, object value)
            {
                // Iterate through possible expressions and ensure only the specified one has a value
                foreach (var expressionProperty in leafExpressionJsonProperties)
                {
                    var parsedValue = expressionProperty.Value.GetValue(leaf);
                    if (expressionProperty.Key.Equals(property, StringComparison.OrdinalIgnoreCase))
                    {
                        if (expressionProperty.Value.PropertyType == typeof(bool?))
                        {
                            Assert.AreEqual(value, (bool)parsedValue);
                        }
                        else if (expressionProperty.Value.PropertyType == typeof(string))
                        {
                            Assert.AreEqual(value, (string)parsedValue);
                        }
                        else
                        {
                            Assert.AreEqual(new JValue(value), parsedValue);
                        }
                    }
                    else
                    {
                        Assert.IsNull(parsedValue);
                    }
                }
            }

            ValidateLeafExpression(expression, operatorProperty, operatorValue);

            if (whereCondition.Length > 0)
            {
                Assert.IsNotNull(expression.Where);
                Assert.IsTrue(expression.Where is LeafExpressionDefinition);
                Assert.AreEqual("whereResource/resourceType", expression.Where.ResourceType);
                Assert.AreEqual("some.json.path.where", expression.Where.Path);
                ValidateLeafExpression(expression.Where as LeafExpressionDefinition, (string)whereCondition[0], whereCondition[1]);
            }
            else
            {
                Assert.IsNull(expression.Where);
            }
        }

        [DataTestMethod]
        [DataRow("allOf", typeof(AllOfExpressionDefinition), DisplayName = "AllOf Expression")]
        [DataRow("anyOf", typeof(AnyOfExpressionDefinition), DisplayName = "AnyOf Expression")]
        [DataRow("allOf", typeof(AllOfExpressionDefinition), "anyOf", typeof(AnyOfExpressionDefinition), DisplayName = "AllOf Expression with Where condition")]
        [DataRow("anyOf", typeof(AnyOfExpressionDefinition), "allOf", typeof(AllOfExpressionDefinition), DisplayName = "AnyOf Expression with Where condition")]
        public void ReadJson_ValidStructuredExpression_ReturnsCorrectTypeAndValues(string expressionName, Type expressionDefinitionType, params object[] whereCondition)
        {
            var expressionTemplate = $@"
                {{
                    ""resourceType"": ""someResource/resourceType"",
                    ""path"": ""some.json.path"",
                    $$where$$
                    ""$$expression$$"": [ 
                        {{
                            ""path"": ""some.other.path"", 
                            ""hasValue"": true 
                        }}, 
                        {{
                            ""path"": ""some.other.path"", 
                            ""equals"": true 
                        }}
                    ]
                }}";

            string optionalWhereBlock = string.Empty;
            if (whereCondition.Length > 0)
            {
                if (whereCondition.Length != 2 || !(whereCondition[0] is string && whereCondition[1] is Type))
                    Assert.Fail($"{nameof(whereCondition)} must contain (only) the operator name and type.");

                optionalWhereBlock = "\"where\": "
                    + expressionTemplate
                        .Replace("$$where$$", "")
                        .Replace("$$expression$$", (string)whereCondition[0])
                        .Replace("someResource", "whereResource")
                        .Replace("some.json.path", "some.where.path")
                    + ",";
            }

            var @object = ReadJson(expressionTemplate.Replace("$$where$$", optionalWhereBlock).Replace("$$expression$$", expressionName));

            Assert.AreEqual(expressionDefinitionType, @object.GetType());

            ExpressionDefinition expressionDefinition = @object as ExpressionDefinition;

            Assert.AreEqual("some.json.path", expressionDefinition.Path);
            Assert.AreEqual("someResource/resourceType", expressionDefinition.ResourceType);

            var expressionArray = expressionDefinition
                .GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .First(p => p.PropertyType == typeof(ExpressionDefinition[]));

            var subExpressions = (ExpressionDefinition[])expressionArray.GetValue(expressionDefinition);
            Assert.AreEqual(2, subExpressions.Length);
            foreach (var e in subExpressions)
            {
                Assert.AreEqual(typeof(LeafExpressionDefinition), e.GetType());
            }

            if (whereCondition.Length > 0)
            {
                Assert.IsNotNull(expressionDefinition.Where);
                Assert.AreEqual((Type)whereCondition[1], expressionDefinition.Where.GetType());
                Assert.AreEqual("whereResource/resourceType", expressionDefinition.Where.ResourceType);
                Assert.AreEqual("some.where.path", expressionDefinition.Where.Path);

                expressionArray = expressionDefinition.Where
                    .GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .First(p => p.PropertyType == typeof(ExpressionDefinition[]));
                subExpressions = (ExpressionDefinition[])expressionArray.GetValue(expressionDefinition.Where);
                Assert.AreEqual(2, subExpressions.Length);
                foreach (var e in subExpressions)
                {
                    Assert.AreEqual(typeof(LeafExpressionDefinition), e.GetType());
                }
            }
            else
            {
                Assert.IsNull(expressionDefinition.Where);
            }
        }

        [DataTestMethod]
        [DataRow("hasValue", "string", DisplayName = "\"HasValue\": \"string\"")]
        [DataRow("exists", new int[0], DisplayName = "\"Exists\": []")]
        [ExpectedException(typeof(JsonReaderException))]
        public void ReadJson_LeafWithInvalidOperator_ThrowsParsingException(string operatorProperty, object operatorValue)
        {
            ReadJson($@"
                {{
                    ""resourceType"": ""resourceType"",
                    ""path"": ""path"",
                    ""{operatorProperty}"": {JsonConvert.SerializeObject(operatorValue)}
                }}");
        }

        [DataTestMethod]
        [DataRow("allOf", "string", DisplayName = "\"AllOf\": \"string\"")]
        [DataRow("anyOf", "string", DisplayName = "\"AnyOf\": \"string\"")]
        [DataRow("allOf", null, DisplayName = "\"AllOf\": null")]
        [DataRow("anyOf", null, DisplayName = "\"AnyOf\": null")]
        [DataRow("allOf", "UseArray", DisplayName = "\"AllOf\": []")]
        [DataRow("anyOf", "UseArray", DisplayName = "\"AnyOf\": []")]
        [DataRow("allOf", "UseArray", null, DisplayName = "\"AllOf\": [ null ]")]
        [DataRow("anyOf", "UseArray", null, DisplayName = "\"AnyOf\": [ null ]")]
        [ExpectedException(typeof(JsonException), AllowDerivedTypes = true)]
        public void ReadJson_StructuredExpressionWithInvalidExpression_ThrowsParsingException(string operatorProperty, object operatorSingleValue, params object[] operatorArrayValue)
        {
            ReadJson($@"
                {{
                    ""resourceType"": ""resourceType"",
                    ""path"": ""path"",
                    ""{operatorProperty}"": {JsonConvert.SerializeObject("UseArray".Equals(operatorSingleValue) ? operatorArrayValue : operatorSingleValue)}
                }}");
        }

        [TestMethod]
        [DataRow(DisplayName = "No operators")]
        [DataRow("hasValue", true, "exists", true, DisplayName = "HasValue and Exists")]
        [ExpectedException(typeof(JsonSerializationException))]
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