// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.PowerShellEngine;
using Microsoft.Azure.Templates.Analyzer.Types;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    public class MockEvaluation : IEvaluation
    {
        public string RuleId { get; set; }

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

    public class MockResult : IResult, IEquatable<MockResult>
    {
        public bool Passed { get; set; }

        public SourceLocation SourceLocation { get; set; }

        public bool Equals(MockResult other)
        {
            return Passed.Equals(other?.Passed)
                && SourceLocation.Equals(other.SourceLocation);
        }

        public override bool Equals(object other)
        {
            var result = other as MockResult;
            return (other != null) && Equals(result);
        }

        public override int GetHashCode()
        {
            return Passed.GetHashCode() ^ SourceLocation.GetHashCode();
        }
    }
}
