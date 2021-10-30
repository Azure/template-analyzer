// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Azure.Templates.Analyzer.Reports
{
    /// <summary>
    /// interface for analyzer reporting modules
    /// </summary>
    public interface IReportWriter : IDisposable
    {
        /// <summary>
        /// Write evaluations to report
        /// </summary>
        /// <param name="templateFile">template file to be analyzed</param>
        /// <param name="evaluations">evaluation list</param>
        void WriteResults(FileInfo templateFile, IEnumerable<Types.IEvaluation> evaluations);
    }
}
