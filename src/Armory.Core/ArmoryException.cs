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
        public ArmoryException()
        {
        }

        public ArmoryException(string message) : base(message)
        {
        }

        public ArmoryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ArmoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}