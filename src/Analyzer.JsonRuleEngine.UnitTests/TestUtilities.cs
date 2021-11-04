// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
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
            var jsonString = $"{{\"Key\": {jsonValue} }}";
            var reader = new JsonTextReader(new StringReader(jsonString))
            {
                DateParseHandling = DateParseHandling.None
            };
            return JObject.Load(reader)["Key"];
        }

        /// <summary>
        /// A mock implementation of an <see cref="Expression"/> for testing internal methods.
        /// </summary>
        internal class MockExpression : Expression
        {
            public Func<IJsonPathResolver, JsonRuleEvaluation> EvaluationCallback { get; set; }
            public Func<IJsonPathResolver, JsonRuleResult> ResultsCallback { get; set; }

            public MockExpression(ExpressionCommonProperties commonProperties)
                : base(commonProperties)
            { }

            /// <summary>
            /// Calls <see cref="Expression.EvaluateInternal(IJsonPathResolver, ILineNumberResolver, Func{IJsonPathResolver, JsonRuleEvaluation})"/> with <see cref="EvaluationCallback"/>,
            /// or <see cref="Expression.EvaluateInternal(IJsonPathResolver, ILineNumberResolver, Func{IJsonPathResolver, JsonRuleResult})"/> with <see cref="ResultsCallback"/>.
            /// <see cref="ResultsCallback"/> is used if it is not null.  Otherwise, <see cref="EvaluationCallback"/> is used.
            /// </summary>
            public override JsonRuleEvaluation Evaluate(IJsonPathResolver jsonScope, ILineNumberResolver lineNumberResolver)
            {
                return ResultsCallback != null
                    ? base.EvaluateInternal(jsonScope, ResultsCallback)
                    : base.EvaluateInternal(jsonScope, EvaluationCallback);
            }
        }
    }
}