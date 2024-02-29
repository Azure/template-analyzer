// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine
{
    /// <summary>
    /// <see cref="JsonRuleEngineException"/> is thrown when there was an issue in Analyzer.RuleEngines.JsonEngine
    /// </summary>
    [Serializable]
    internal class JsonRuleEngineException : Exception
    {
        /// <summary>
        /// Default constructor for a <see cref="JsonRuleEngineException"/>
        /// </summary>
        public JsonRuleEngineException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="JsonRuleEngineException"/>
        /// </summary>
        /// <param name="message">The exception message associated with the exception</param>
        public JsonRuleEngineException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new <see cref="JsonRuleEngineException"/>
        /// </summary>
        /// <param name="message">The exception message associated with the exception</param>
        /// <param name="innerException">The inner exception wrapped by the <see cref="JsonRuleEngineException"/></param>
        public JsonRuleEngineException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}