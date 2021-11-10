// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    public class MockEvaluation : Types.IEvaluation
    {
        public string RuleId { get; set; }

        public string RuleDescription { get; set; }

        public string Recommendation { get; set; }

        public string HelpUri { get; set; }

        public string FileIdentifier { get; set; }

        public bool Passed { get; set; }

        public IEnumerable<Types.IEvaluation> Evaluations { get; set; }

        public IEnumerable<Types.IResult> Results { get; set; }

        public bool HasResults { get; set; }
    }

    public class MockResult : Types.IResult
    {
        public bool Passed { get; set; }

        public int LineNumber { get; set; }
    }
}
