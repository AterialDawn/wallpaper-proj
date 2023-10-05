using System;

namespace player.Core.Service
{
    class ServiceInitializationException : Exception
    {
        public ServiceInitializationException()
        {
        }

        public ServiceInitializationException(string message)
        : base(message)
        {
        }

        public ServiceInitializationException(string message, Exception inner)
        : base(message, inner)
        {
        }
    }
}
