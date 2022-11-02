// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    public class MockEvaluation : IEvaluation
    {
        public string RuleId { get; set; }

        public string RuleName { get; set; }

        public string RuleDescription { get; set; }

        public string Recommendation { get; set; }

        public string HelpUri { get; set; }

        public Severity Severity { get; set; }

        public string FileIdentifier { get; set; }

        public bool Passed { get; set; }

        public IEnumerable<IEvaluation> Evaluations { get; set; }

        public IResult Result { get; set; }

        public bool HasResults { get; set; }
    }

    public class MockResult : IResult
    {
        public bool Passed { get; set; }

        public int LineNumber { get; set; }
    }
}
