// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    // Mock ILineNumberResolver for use in tests
    public class MockLineResolver : ILineNumberResolver
    {
        public int ResolveLineNumber(string path) => 0;
    }

    [TestClass]
    public class RuleParsingTests
    {
        [DataTestMethod]
        [DataRow("hasValue", false, typeof(HasValueOperator), DisplayName = "HasValue: false")]
        [DataRow("exists", true, typeof(ExistsOperator), DisplayName = "Exists: true")]
        // [DataRow("greater", "2021-02-28", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28")] // FIXME
        [DataRow("greater", "2021-02-28T18:17:16Z", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28T18:17:16Z")]
        [DataRow("greater", "2021-02-28T18:17:16.543Z", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28T18:17:16.543Z")]
        // [DataRow("greater", "20210228T181716Z", typeof(InequalityOperator), DisplayName = "Greater: 20210228T181716Z")] // FIXME
        [DataRow("greater", "2021-02-28T18:17:16+00:00", typeof(InequalityOperator), DisplayName = "Greater: 2021-02-28T18:17:16+00:00")]
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

            var leafExpression = leafDefinition.ToExpression(new MockLineResolver()) as LeafExpression;
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

            Assert.AreEqual(JTokenType.Date, inequalityOperator.SpecifiedValue.Type);

            var expectedDate = DateTime.Parse(operatorValue, styles: DateTimeStyles.RoundtripKind);
            var parsedDate = inequalityOperator.SpecifiedValue.Value<DateTime>();

            Assert.AreEqual(expectedDate.Year, parsedDate.Year);
            Assert.AreEqual(expectedDate.Month, parsedDate.Month);
            Assert.AreEqual(expectedDate.Day, parsedDate.Day);
            Assert.AreEqual(expectedDate.Hour, parsedDate.Hour);
            Assert.AreEqual(expectedDate.Minute, parsedDate.Minute);
            Assert.AreEqual(expectedDate.Second, parsedDate.Second);
            Assert.AreEqual(expectedDate.Millisecond, parsedDate.Millisecond);
        }

        private const string TestResourceType = "Namespace/ResourceType";
        private const string TestPath = "json.path";
    }
}
