// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class LeafExpressionDefinitionTests
    {
        [DataTestMethod]
        [DataRow(true, DisplayName = "Value is true")]
        [DataRow(false, DisplayName = "Value is false")]
        public void ToExpression_HasValueOperator_ReturnsLeafExpressionWithHasValue(bool operatorValue)
        {
            var leafExpression = GenerateLeafExpression(leaf => leaf.HasValue = operatorValue);

            var leafOperator = leafExpression.Operator as HasValueOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.AreEqual(operatorValue, leafOperator.EffectiveValue);
            Assert.IsFalse(leafOperator.IsNegative);
        }

        [DataTestMethod]
        [DataRow(true, DisplayName = "Value is true")]
        [DataRow(false, DisplayName = "Value is false")]
        public void ToExpression_ExistsOperator_ReturnsLeafExpressionWithExists(bool operatorValue)
        {
            var leafExpression = GenerateLeafExpression(leaf => leaf.Exists = operatorValue);

            var leafOperator = leafExpression.Operator as ExistsOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.AreEqual(operatorValue, leafOperator.EffectiveValue);
            Assert.IsFalse(leafOperator.IsNegative);
        }

        [TestMethod]
        public void ToExpression_EqualsOperator_ReturnsLeafExpressionWithEquals()
        {
            string operatorValue = "string";
            var leafExpression = GenerateLeafExpression(leaf => leaf.Is = operatorValue);

            var leafOperator = leafExpression.Operator as EqualsOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.IsFalse(leafOperator.IsNegative);

            leafExpression = GenerateLeafExpression(leaf => leaf.NotEquals = operatorValue);

            leafOperator = leafExpression.Operator as EqualsOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.IsTrue(leafOperator.IsNegative);
        }

        [TestMethod]
        public void ToExpression_RegexOperator_ReturnsLeafExpressionWithRegex()
        {
            string operatorValue = "regexPattern";
            var leafExpression = GenerateLeafExpression(leaf => leaf.Regex = operatorValue);

            var leafOperator = leafExpression.Operator as RegexOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.IsFalse(leafOperator.IsNegative);
        }

        [TestMethod]
        public void ToExpression_InOperator_ReturnsLeafExpressionWithIn()
        {
            string[] possibleValues = {"aValue", "anotherValue"};
            var operatorValue = JArray.FromObject(possibleValues);

            var leafExpression = GenerateLeafExpression(leaf => leaf.In = operatorValue);

            var leafOperator = leafExpression.Operator as InOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(operatorValue, leafOperator.SpecifiedValue);
            Assert.IsFalse(leafOperator.IsNegative);
        }

        [TestMethod]
        public void ToExpression_InequalityOperator_ReturnsLeafExpressionWithInequality()
        {
            var operatorValue = 100;

            var leafExpression = GenerateLeafExpression(leaf => leaf.Greater = operatorValue);
            var leafOperator = leafExpression.Operator as InequalityOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.IsTrue(leafOperator.Greater);
            Assert.IsFalse(leafOperator.OrEquals);
            Assert.AreEqual(100, leafOperator.EffectiveValue);

            leafExpression = GenerateLeafExpression(leaf => leaf.Less = operatorValue);
            leafOperator = leafExpression.Operator as InequalityOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.IsFalse(leafOperator.Greater);
            Assert.IsFalse(leafOperator.OrEquals);
            Assert.AreEqual(100, leafOperator.EffectiveValue);

            leafExpression = GenerateLeafExpression(leaf => leaf.GreaterOrEquals = operatorValue);
            leafOperator = leafExpression.Operator as InequalityOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.IsTrue(leafOperator.Greater);
            Assert.IsTrue(leafOperator.OrEquals);
            Assert.AreEqual(100, leafOperator.EffectiveValue);

            leafExpression = GenerateLeafExpression(leaf => leaf.LessOrEquals = operatorValue);
            leafOperator = leafExpression.Operator as InequalityOperator;
            Assert.IsNotNull(leafOperator);
            Assert.AreEqual(new JValue(operatorValue), leafOperator.SpecifiedValue);
            Assert.IsFalse(leafOperator.Greater);
            Assert.IsTrue(leafOperator.OrEquals);
            Assert.AreEqual(100, leafOperator.EffectiveValue);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void ToExpression_NoOperators_ThrowsException()
        {
            new LeafExpressionDefinition().ToExpression(new Mock<ILineNumberResolver>().Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ToExpression_NullLineNumberResolver_ThrowsException()
        {
            new LeafExpressionDefinition { Exists = true }.ToExpression(null);
        }

        private LeafExpression GenerateLeafExpression(Action<LeafExpressionDefinition> propertySetter)
        {
            var leaf = new LeafExpressionDefinition
            {
                Path = "json.path",
                ResourceType = "Namespace/ResourceType"
            };
            propertySetter(leaf);
            
            var expression = leaf.ToExpression(new Mock<ILineNumberResolver>().Object) as LeafExpression;
            Assert.AreEqual("json.path", expression.Path);
            Assert.AreEqual("Namespace/ResourceType", expression.ResourceType);

            return expression;
        }
    }
}
