// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;

namespace Armory.Core
{
    /// <summary>
    /// Armory Exception is thrown when there was an issue in Armory.Core
    /// </summary>
    [Serializable]
    internal class ArmoryException : Exception
    {
        /// <summary>
        /// Default constructor for an ArmoryException
        /// </summary>
        public ArmoryException()
        {
        }

        /// <summary>
        /// Creates a new ArmoryException
        /// </summary>
        /// <param name="message">The exception message associated with the exception</param>
        public ArmoryException(string message) 
            : base(message)
        {
        }

        /// <summary>
        /// Creates a new ArmoryException
        /// </summary>
        /// <param name="message">The exception message associated with the exception</param>
        /// <param name="innerException">The inner exception wrapped by the ArmoryException</param>
        public ArmoryException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }

        protected ArmoryException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}