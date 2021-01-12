// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Templates.Analyzer.Core
{
    /// <summary>
    /// Template Analyzer Exception is thrown when there was an issue in Analyzer.Core
    /// </summary>
    [Serializable]
    internal class TemplateAnalyzerException : Exception
    {
        /// <summary>
        /// Default constructor for an TemplateAnalyzerException
        /// </summary>
        public TemplateAnalyzerException()
        {
        }

        /// <summary>
        /// Creates a new TemplateAnalyzerException
        /// </summary>
        /// <param name="message">The exception message associated with the exception</param>
        public TemplateAnalyzerException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new TemplateAnalyzerException
        /// </summary>
        /// <param name="message">The exception message associated with the exception</param>
        /// <param name="innerException">The inner exception wrapped by the TemplateAnalyzerException</param>
        public TemplateAnalyzerException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        protected TemplateAnalyzerException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}