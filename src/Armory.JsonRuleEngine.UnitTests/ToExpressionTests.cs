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
    public class OperatorSpecificValidatorAttribute : Attribute { }

    [TestClass]
    public class ToExpressionTests
    {
        [DataTestMethod]
        [DataRow("hasValue", false, typeof(HasValueOperator))]
        [DataRow("exists", true, typeof(ExistsOperator))]
        public void ToExpressionTests_ValidOperator_ReturnsLeafExpressionWithCorrectOperator(string operatorProperty, object operatorValue, Type operatorType)
        {
            // Generate JSON, parse, and validate parsed LeafExpression
            var leafOperator = ParseJsonValidateAndReturnOperator(operatorProperty, operatorValue);

            // Verify operator in parsed leaf expression is expected type
            Assert.IsTrue(leafOperator.GetType() == operatorType);

            // Run operator-specific validation
            MethodInfo operatorSpecificValidator = GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                .Where(method => method.GetCustomAttribute<OperatorSpecificValidatorAttribute>() != null)
                .FirstOrDefault(method => method.GetParameters().ToList().Exists(p => p.ParameterType == operatorType));

            Assert.IsNotNull(operatorSpecificValidator, $"Unable to find a validation method for LeafExpressionOperator {operatorType}");
            operatorSpecificValidator.Invoke(this, new[] { leafOperator, operatorValue });
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
                operatorValue is string ? $"\"{jsonValue}\"" : jsonValue));

            var leafExpression = leafDefinition.ToExpression() as LeafExpression;
            Assert.IsNotNull(leafExpression);
            Assert.AreEqual(TestPath, leafExpression.Path);
            Assert.AreEqual(TestResourceType, leafExpression.ResourceType);
            Assert.IsNotNull(leafExpression.Operator);

            return leafExpression.Operator;
        }

        [OperatorSpecificValidator]
        private static void HasValueValidation(HasValueOperator hasValueOperator, bool operatorValue)
        {
            Assert.AreEqual(operatorValue, hasValueOperator.EffectiveValue);
            Assert.IsFalse(hasValueOperator.IsNegative);
        }

        [OperatorSpecificValidator]
        private static void ExistsValidation(ExistsOperator existsOperator, bool operatorValue)
        {
            Assert.AreEqual(operatorValue, existsOperator.EffectiveValue);
            Assert.IsFalse(existsOperator.IsNegative);
        }

        private const string TestResourceType = "Namespace/ResourceType";
        private const string TestPath = "json.path";
    }
}
