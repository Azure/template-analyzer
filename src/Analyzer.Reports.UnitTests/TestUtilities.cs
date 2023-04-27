// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.IO.Abstractions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.CodeAnalysis.Sarif;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    /// <summary>
    /// Simple wrapper class to enable mocking for System.IO.Abstractions.FileSystemStream for unit testing.
    /// </summary>
    internal class MockFileStream : FileSystemStream
    {
        public MockFileStream(Stream stream) : this(stream, "unusedTestPath", false)
        {
        }

        public MockFileStream(Stream stream, string path, bool isAsync) : base(stream, path, isAsync)
        {
        }
    }

    internal class Utilities
    {
        public static FailureLevel GetLevelFromSeverity(Severity severity) =>
            severity switch
            {
                Severity.Low => FailureLevel.Note,
                Severity.Medium => FailureLevel.Warning,
                Severity.High => FailureLevel.Error,
                _ => FailureLevel.Error,
            };
    }
}
