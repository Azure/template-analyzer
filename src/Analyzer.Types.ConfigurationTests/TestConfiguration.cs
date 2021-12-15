// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

//TODO : Update

namespace Analyzer.Types.ConfigurationTests
{
    [JsonObject]
    public class TestConfiguration
    {
        [JsonProperty]
        public string Template;

        [JsonProperty]
        public ExpectedRuleFailure[] ReportedFailures;

        public string TestName { get; set; }
    }

    [JsonObject]
    public class ExpectedRuleFailure
    {
        [JsonProperty]
        public int LineNumber;
    }
}
