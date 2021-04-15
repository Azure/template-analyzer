// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
{
    /// <summary>
    /// Template Analyzer Exception is thrown when there was an issue in Analyzer.Core
    /// </summary>
    [Serializable]
    internal class JsonRuleEngineException : Exception
    {
        /// <summary>
        /// Default constructor for an TemplateAnalyzerException
        /// </summary>
        public JsonRuleEngineException()
        {
        }

        /// <summary>
        /// Creates a new TemplateAnalyzerException
        /// </summary>
        /// <param name="message">The exception message associated with the exception</param>
        public JsonRuleEngineException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new TemplateAnalyzerException
        /// </summary>
        /// <param name="message">The exception message associated with the exception</param>
        /// <param name="innerException">The inner exception wrapped by the TemplateAnalyzerException</param>
        public JsonRuleEngineException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        protected JsonRuleEngineException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}