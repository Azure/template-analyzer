// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.FunctionalTests
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
    public class RuleParsingTests
    {
        [DataTestMethod]
        [DataRow("hasValue", false, typeof(HasValueOperator), DisplayName = "HasValue: false")]
        [DataRow("exists", true, typeof(ExistsOperator), DisplayName = "Exists: true")]
        [DataRow("greater", "2021-02-28", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28")] 
        [DataRow("greater", "2021-02-28T18:17:16Z", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28T18:17:16Z")]
        [DataRow("greater", "2021-02-28T18:17:16+00:00", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28T18:17:16+00:00")] 
        [DataRow("greater", "2021-02-28T18:17Z", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28T18:17Z")] 
        [DataRow("greater", "2021-02-28T18:17+00:00", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28T18:17+00:00")] 
        [DataRow("greater", "2021-02-28 18:17:16Z", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28 18:17:16Z")]
        [DataRow("greater", "2021-02-28 18:17:16+00:00", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28 18:17:16+00:00")]
        public void DeserializeExpression_LeafWithValidOperator_ReturnsLeafExpressionWithCorrectOperator(string operatorProperty, object operatorValue, Type operatorType)
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

            var leafExpression = leafDefinition.ToExpression() as LeafExpression;
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

        [OperatorSpecificValidator(typeof(InequalityOperator))]
        private static void InequalityValidation(InequalityOperator inequalityOperator, string operatorValue)
        {
            Assert.IsFalse(inequalityOperator.IsNegative);
            Assert.IsTrue(inequalityOperator.Greater);
            Assert.IsFalse(inequalityOperator.OrEquals);

            var parsedDate = DateTime.FromOADate(inequalityOperator.EffectiveValue);

            Assert.AreEqual(2021, parsedDate.Year);
            Assert.AreEqual(2, parsedDate.Month);
            // Not checking the day and hour because converting to OADate loses localization information
            Assert.IsTrue(parsedDate.Minute == 0 || parsedDate.Minute == 17);
            Assert.IsTrue(parsedDate.Second == 0 || parsedDate.Second == 16);
        }

        private const string TestResourceType = "Namespace/ResourceType";
        private const string TestPath = "json.path";
    }
}