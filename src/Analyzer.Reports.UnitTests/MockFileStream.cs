using System.IO;
using System.IO.Abstractions;

namespace Microsoft.Azure.Templates.Analyzer.Reports.UnitTests
{
    /// <summary>
    /// Simple wrapper class to enable mocking for System.IO.Abstractions.FileSystemStream for unit testing
    /// </summary>
    public class MockFileStream : FileSystemStream
    {
        public MockFileStream(Stream stream) : this(stream, "unusedTestPath", false)
        {
        }

        public MockFileStream(Stream stream, string path, bool isAsync) : base(stream, path, isAsync)
        {
        }
    }
}
