using System;
using System.Net;

namespace S3WebApi.Helpers
{
    public class HttpStatusCodeException : Exception
    {
        public int StatusCode { get; }

        public HttpStatusCodeException(int statusCode)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCodeException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCodeException(int statusCode, string message, Exception inner) : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }
}
