// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Templates.Analyzer.Types;
using Newtonsoft.Json;

namespace Analyzer.Types.ConfigurationTests
{
    [JsonObject]
    public class TestConfiguration
    {
        [JsonProperty]
        public string Template;

        [JsonProperty]
        public string Configuration;

        [JsonProperty]
        public ExpectedRuleFailure[] ReportedFailures;

        public string TestName { get; set; }
    }

    [JsonObject]
    public class ExpectedRuleFailure
    {
        [JsonProperty]
        public string Id;

        [JsonProperty]
        public Severity Severity;
    }
}
