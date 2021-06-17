// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    /// <summary>
    /// A set of commonly used utilities in the tests.
    /// </summary>
    public class TestUtilities
    {
        /// <summary>
        /// Creates JSON with 'value' as the value of a key, parses it, then selects that key.
        /// </summary>
        public static JToken ToJToken(object value)
        {
            var jsonValue = JsonConvert.SerializeObject(value);
            return JToken.Parse($"{{\"Key\": {jsonValue} }}")["Key"];
        }

        /// <summary>
        /// A mock implementation of an <see cref="Expression"/> for testing internal methods.
        /// </summary>
        internal class MockExpression : Expression
        {
            public Func<IJsonPathResolver, JsonRuleEvaluation> EvaluationCallback { get; set; }

            public MockExpression(ExpressionCommonProperties commonProperties)
                : base(commonProperties)
            { }

            public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope)
            {
                return base.EvaluateInternal(jsonScope, EvaluationCallback);
            }
        }
    }
}