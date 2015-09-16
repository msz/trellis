using System;
using System.Runtime.Serialization;

namespace Trellis.Core
{
    [Serializable]
    internal class IdException : Exception
    {
        public IdException()
        {
        }

        public IdException(string message) : base(message)
        {
        }

        public IdException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}