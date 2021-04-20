// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    public class JsonRuleEngineTestsUtilities
    {
        // Creates JSON with 'value' as the value of a key, parses it, then selects that key.
        public static JToken ToJToken(object value)
        {
            var jsonValue = JsonConvert.SerializeObject(value);
            return JToken.Parse($"{{\"Key\": {jsonValue} }}")["Key"];
        }
    }
}