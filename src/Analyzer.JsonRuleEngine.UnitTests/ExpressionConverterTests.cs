// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
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

        // Test data for 'in' operator test. Needed in order to use an array as parameter
        public static IReadOnlyList<object[]> InOperatorTestScenarios { get; } = new List<object[]>
        {
            new object[] { "in", new object[] { "anotherValue", "aValue"} }
        }.AsReadOnly();

        // Returns the DisplayName for 'in' operator test. If this was stored in InOperatorTestScenarios, ReadJson_LeafWithValidOperator_ReturnsCorrectTypeAndValues would interpret it as part of the whereCondition
        public static string GetInOperatorTestDisplayName(MethodInfo _, object[] data) => "{\"In\": [\"anotherValue\", \"aValue\"]}";

        [DataTestMethod]
        [DataRow("hasValue", true, DisplayName = "{\"HasValue\": true}")]
        [DataRow("hasValue", false, "hasValue", true, DisplayName = "{\"HasValue\": false}, Where: {\"HasValue\": true}")]
        [DataRow("exists", false, DisplayName = "{\"Exists\": false}")]
        [DataRow("equals", "someString", DisplayName = "{\"Equals\": \"someString\"}")]
        [DataRow("equals", "someString", "exists", true, DisplayName = "{\"Equals\": \"someString\"}, Where: {\"Exists\": true}")]
        [DataRow("notEquals", 0, DisplayName = "{\"NotEquals\": 0}")]
        [DataRow("regex", "regexPattern", DisplayName = "{\"Regex\": \"regexPattern\"}")]
        [DataRow("greater", 100, DisplayName = "{\"Greater\": 100}")]
        [DataRow("less", 100, DisplayName = "{\"Less\": 100}")]
        [DataRow("greaterOrEquals", 100, DisplayName = "{\"GreaterOrEquals\": 100}")]
        [DataRow("lessOrEquals", 100, DisplayName = "{\"LessOrEquals\": 100}")]
        [DataRow("greater", "2021-02-28T18:17:16.543Z", DisplayName = "{\"Greater\": 2021-02-28T18:17:16.543Z}")]
        [DataRow("less", "2021-02-28T18:17:16.543Z", DisplayName = "{\"Less\": 2021-02-28T18:17:16.543Z}")]
        [DataRow("greaterOrEquals", "2021-02-28T18:17:16.543Z", DisplayName = "{\"GreaterOrEquals\": 2021-02-28T18:17:16.543Z}")]
        [DataRow("lessOrEquals", "2021-02-28T18:17:16.543Z", DisplayName = "{\"LessOrEquals\": 2021-02-28T18:17:16.543Z}")]
        [DynamicData(nameof(InOperatorTestScenarios), DynamicDataDisplayName = nameof(GetInOperatorTestDisplayName))]
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

            // Local function to validate properties of leaf expression
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
                            var valueAsJson = TestUtilities.ToJToken(value);
                            Assert.IsTrue(JToken.DeepEquals(valueAsJson, (JToken)parsedValue));
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

            // If test should use a 'where' condition, create the expression to put in it
            string optionalWhereBlock = string.Empty;
            if (whereCondition.Length > 0)
            {
                if (whereCondition.Length != 2 || !(whereCondition[0] is string && whereCondition[1] is Type))
                    Assert.Fail($"{nameof(whereCondition)} must contain (only) the operator name and type.");

                // Replace values to make it valid and unique from the outer expression
                optionalWhereBlock = "\"where\": "
                    + expressionTemplate
                        .Replace("$$where$$", "")
                        .Replace("$$expression$$", (string)whereCondition[0])
                        .Replace("someResource", "whereResource")
                        .Replace("some.json.path", "some.where.path")
                    + ",";
            }

            // Parse expression
            var @object = ReadJson(expressionTemplate.Replace("$$where$$", optionalWhereBlock).Replace("$$expression$$", expressionName));

            // Local function to validate expression and potential inner 'where' expression
            void ValidateExpression(ExpressionDefinition expression, Type expectedSpecificType, string expectedResourceType, string expectedPath)
            {
                // Validate specific type and string properties
                Assert.AreEqual(expectedSpecificType, expression.GetType());
                Assert.AreEqual(expectedResourceType, expression.ResourceType);
                Assert.AreEqual(expectedPath, expression.Path);

                var expressionArray = expression
                    .GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .First(p => p.PropertyType == typeof(ExpressionDefinition[]));

                // Validate expressions within the structured operator
                var subExpressions = (ExpressionDefinition[])expressionArray.GetValue(expression);
                Assert.AreEqual(2, subExpressions.Length);
                foreach (var e in subExpressions)
                {
                    Assert.AreEqual(typeof(LeafExpressionDefinition), e.GetType());
                }
            }

            Assert.AreEqual(expressionDefinitionType, @object.GetType());

            // Validate top-level expression
            ExpressionDefinition expressionDefinition = @object as ExpressionDefinition;
            ValidateExpression(expressionDefinition, expressionDefinitionType, "someResource/resourceType", "some.json.path");

            // Validate where condition, if specified
            if (whereCondition.Length > 0)
            {
                Assert.IsNotNull(expressionDefinition.Where);
                ValidateExpression(expressionDefinition.Where, (Type)whereCondition[1], "whereResource/resourceType", "some.where.path");
            }
            else
            {
                Assert.IsNull(expressionDefinition.Where);
            }
        }

        [DataTestMethod]
        [DataRow(new object[] { }, DisplayName = "Not Expression")]
        [DataRow(new object[] { "not", typeof(NotExpressionDefinition) }, DisplayName = "Not Expression with Where condition")]
        public void ReadJson_ValidNotExpression_ReturnsCorrectTypeAndValues(object[] whereCondition)
        {
            var expressionTemplate = $@"
                {{
                    ""resourceType"": ""someResource/resourceType"",
                    ""path"": ""some.json.path"",
                    $$where$$
                    ""not"":
                        {{
                            ""path"": ""some.other.path"", 
                            ""hasValue"": true 
                        }}
                }}";

            // If test should use a 'where' condition, create the expression to put in it
            string optionalWhereBlock = string.Empty;
            if (whereCondition.Length > 0)
            {
                if (whereCondition.Length != 2 || !(whereCondition[0] is string && whereCondition[1] is Type))
                    Assert.Fail($"{nameof(whereCondition)} must contain (only) the operator name and type.");

                // Replace values to make it valid and unique from the outer expression
                optionalWhereBlock = "\"where\": "
                    + expressionTemplate
                        .Replace("$$where$$", "")
                        .Replace("someResource", "whereResource")
                        .Replace("some.json.path", "some.where.path")
                    + ",";
            }

            // Parse expression
            var @object = ReadJson(expressionTemplate.Replace("$$where$$", optionalWhereBlock));

            // Local function to validate expression and potential inner 'where' expression
            static void ValidateExpression(ExpressionDefinition expression, Type expectedSpecificType, string expectedResourceType, string expectedPath)
            {
                // Validate specific type and string properties
                Assert.AreEqual(expectedSpecificType, expression.GetType());
                Assert.AreEqual(expectedResourceType, expression.ResourceType);
                Assert.AreEqual(expectedPath, expression.Path);
                Assert.AreEqual(typeof(LeafExpressionDefinition), (expression as NotExpressionDefinition).Not.GetType());
            }

            Assert.AreEqual(typeof(NotExpressionDefinition), @object.GetType());

            // Validate top-level expression
            ExpressionDefinition expressionDefinition = @object as ExpressionDefinition;
            ValidateExpression(expressionDefinition, typeof(NotExpressionDefinition), "someResource/resourceType", "some.json.path");

            // Validate where condition, if specified
            if (whereCondition.Length > 0)
            {
                Assert.IsNotNull(expressionDefinition.Where);
                ValidateExpression(expressionDefinition.Where, (Type)whereCondition[1], "whereResource/resourceType", "some.where.path");
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
            ReadSimpleOperatorJson(operatorProperty, operatorValue);
        }

        [DataTestMethod]
        [DataRow("in", 7, DisplayName = "\"In\": 7")]
        [DataRow("in", 5.7, DisplayName = "\"In\": 5.7")]
        [DataRow("in", "aString", DisplayName = "\"In\": \"aString\"")]
        [DataRow("in", true, DisplayName = "\"In\": true")]
        [ExpectedException(typeof(JsonSerializationException))]
        public void ReadJson_LeafWithInvalidOperator_ThrowsSerializationException(string operatorProperty, object operatorValue)
        {
            ReadSimpleOperatorJson(operatorProperty, operatorValue);
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
            ReadSimpleOperatorJson(operatorProperty, "UseArray".Equals(operatorSingleValue) ? operatorArrayValue : operatorSingleValue);
        }

        [TestMethod]
        [DataRow(new object[] { }, DisplayName = "No operators")]
        [DataRow(["hasValue", true, "exists", true], DisplayName = "HasValue and Exists")]
        [ExpectedException(typeof(JsonSerializationException))]
        public void ReadJson_LeafWithInvalidOperatorCount_ThrowsParsingException(object[] operators)
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
        public void ReadJson_DateTokenType_DoesNotParseDate()
        {
            var @object = ReadJson($@"
                {{
                    ""resourceType"": ""someResource/resourceType"",
                    ""path"": ""some.json.path"",
                    ""greater"": ""2021-02-28T18:17:16Z"",
                }}");

            Assert.AreEqual(typeof(LeafExpressionDefinition), @object.GetType());

            var expressionDefinition = @object as LeafExpressionDefinition;

            Assert.AreEqual(JTokenType.String, expressionDefinition.Greater.Type);
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
                new JsonTextReader(new StringReader(jsonString)),
                typeof(ExpressionDefinition),
                null,
                JsonSerializer.CreateDefault());

        private static void ReadSimpleOperatorJson(string operatorProperty, object operatorValue)
        {
            ReadJson($@"
                {{
                    ""resourceType"": ""resourceType"",
                    ""path"": ""path"",
                    ""{operatorProperty}"": {JsonConvert.SerializeObject(operatorValue)}
                }}");
        }
    }
}