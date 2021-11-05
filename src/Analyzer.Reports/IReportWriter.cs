// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
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
        /// <param name="evaluations">Evaluation list</param>
        /// <param name="templateFile">Template file to be analyzed</param>
        /// <param name="parametersFile">The parameter file to use when parsing the specified ARM template.</param>
        void WriteResults(IEnumerable<Types.IEvaluation> evaluations, IFileInfo templateFile, IFileInfo parametersFile = null);
    }
}
