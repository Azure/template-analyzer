// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Armory.JsonRuleEngine.UnitTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OperatorSpecificValidatorAttribute : Attribute
    {
        public Type Operator { get; set; }

        public OperatorSpecificValidatorAttribute(Type @operator)
        {
            this.Operator = @operator;
        }
    }

    [TestClass]
    public class ExpressionDefinitionTests
    {
        [DataTestMethod]
        [DataRow("hasValue", false, typeof(HasValueOperator), DisplayName = "HasValue: false")]
        [DataRow("exists", true, typeof(ExistsOperator), DisplayName = "Exists: true")]
        public void ToExpression_LeafWithValidOperator_ReturnsLeafExpressionWithCorrectOperator(string operatorProperty, object operatorValue, Type operatorType)
        {
            // Generate JSON, parse, and validate parsed LeafExpression
            var leafOperator = ParseJsonValidateAndReturnOperator(operatorProperty, operatorValue);

            // Verify operator in parsed leaf expression is expected type
            Assert.IsTrue(leafOperator.GetType() == operatorType);

            // Run operator-specific validation
            MethodInfo operatorSpecificValidator = GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<OperatorSpecificValidatorAttribute>()?.Operator == operatorType)
                .FirstOrDefault();

            Assert.IsNotNull(operatorSpecificValidator, $"Unable to find a validation method for LeafExpressionOperator {operatorType}");
            operatorSpecificValidator.Invoke(this, new[] { leafOperator, operatorValue });
        }

        [DataTestMethod]
        [DataRow("hasValue", "string", DisplayName = "HasValue: \"string\"")]
        [DataRow("exists", new int[0], DisplayName = "Exists: []")]
        [ExpectedException(typeof(JsonReaderException))]
        public void ToExpression_LeafWithInvalidOperator_ThrowsParsingException(string operatorProperty, object operatorValue)
        {
            ParseJsonValidateAndReturnOperator(operatorProperty, operatorValue);
        }

        [TestMethod]
        [DataRow(DisplayName = "No operators")]
        [DataRow("hasValue", true, "exists", true, DisplayName = "HasValue and Exists")]
        [ExpectedException(typeof(JsonException))]
        public void ToExpression_LeafWithInvalidOperatorCount_ThrowsParsingException(params object[] operators)
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
                    leafDefinition = leafDefinition + $", \"{op}\": ";
                }
                else
                {
                    var jsonValue = JsonConvert.SerializeObject(op);
                    leafDefinition = leafDefinition + jsonValue;
                }
            }

            leafDefinition = leafDefinition + "}";

            try
            {
                JsonConvert.DeserializeObject<ExpressionDefinition>(leafDefinition);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.IndexOf(operators.Length > 0 ? "too many" : "invalid", StringComparison.OrdinalIgnoreCase) >= 0);
                throw;
            }
        }

        private LeafExpressionOperator ParseJsonValidateAndReturnOperator(string operatorProperty, object operatorValue)
        {
            var jsonValue = JsonConvert.SerializeObject(operatorValue);
            var leafDefinition = JsonConvert.DeserializeObject<ExpressionDefinition>(string.Format(@"
                {{
                    ""resourceType"": ""{0}"",
                    ""path"": ""{1}"",
                    ""{2}"": {3}
                }}",
                TestResourceType,
                TestPath,
                operatorProperty,
                jsonValue));

            var leafExpression = leafDefinition.ToExpression(new RuleDefinition()) as LeafExpression;
            Assert.IsNotNull(leafExpression);
            Assert.AreEqual(TestPath, leafExpression.Path);
            Assert.AreEqual(TestResourceType, leafExpression.ResourceType);
            Assert.IsNotNull(leafExpression.Operator);

            return leafExpression.Operator;
        }

        [OperatorSpecificValidator(typeof(HasValueOperator))]
        private static void HasValueValidation(HasValueOperator hasValueOperator, bool operatorValue)
        {
            Assert.AreEqual(operatorValue, hasValueOperator.EffectiveValue);
            Assert.IsFalse(hasValueOperator.IsNegative);
        }

        [OperatorSpecificValidator(typeof(ExistsOperator))]
        private static void ExistsValidation(ExistsOperator existsOperator, bool operatorValue)
        {
            Assert.AreEqual(operatorValue, existsOperator.EffectiveValue);
            Assert.IsFalse(existsOperator.IsNegative);
        }

        private const string TestResourceType = "Namespace/ResourceType";
        private const string TestPath = "json.path";
    }
}
