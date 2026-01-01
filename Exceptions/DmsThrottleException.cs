using System;

namespace S3WebApi.Exceptions
{
    public class DmsThrottleException : Exception
    {
        public DmsThrottleException(string errorMessage)
            : base(errorMessage)
        {
        }
    }
}
