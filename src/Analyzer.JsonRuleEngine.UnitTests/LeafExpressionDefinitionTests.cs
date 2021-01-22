// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Schemas;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        [ExpectedException(typeof(NotImplementedException))]
        public void ToExpression_NoOperators_ThrowsException()
        {
            new LeafExpressionDefinition().ToExpression();
        }

        private LeafExpression GenerateLeafExpression(Action<LeafExpressionDefinition> propertySetter)
        {
            var leaf = new LeafExpressionDefinition
            {
                Path = "json.path",
                ResourceType = "Namespace/ResourceType"
            };
            propertySetter(leaf);
            
            var expression = leaf.ToExpression() as LeafExpression;
            Assert.AreEqual("json.path", expression.Path);
            Assert.AreEqual("Namespace/ResourceType", expression.ResourceType);

            return expression;
        }
    }
}
