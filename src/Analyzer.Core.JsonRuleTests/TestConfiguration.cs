// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace Microsoft.Azure.Templates.Analyzer.Core.JsonRuleTests
{
    [JsonObject]
    public class TestConfiguration
    {
        [JsonProperty]
        public string Template;

        [JsonProperty]
        public bool ExpectPass;

        [JsonProperty]
        public int[] ReportedLines;

        public string TestName { get; set; }
    }
}
