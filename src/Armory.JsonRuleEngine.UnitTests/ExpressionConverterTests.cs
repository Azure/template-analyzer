// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Armory.JsonRuleEngine.UnitTests
{
    [TestClass]
    public class ExpressionConverterTests
    {
        [DataTestMethod]
        [DataRow("hasValue", "string", DisplayName = "HasValue: \"string\"")]
        [DataRow("exists", new int[0], DisplayName = "Exists: []")]
        [ExpectedException(typeof(JsonReaderException))]
        public void ReadJson_LeafWithInvalidOperator_ThrowsParsingException(string operatorProperty, object operatorValue)
        {
            JsonConvert.DeserializeObject<ExpressionDefinition>(string.Format(@"
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
                JsonConvert.DeserializeObject<ExpressionDefinition>(leafDefinition);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.IndexOf(operators.Length > 0 ? "too many" : "invalid", StringComparison.OrdinalIgnoreCase) >= 0);
                throw;
            }
        }
    }
}