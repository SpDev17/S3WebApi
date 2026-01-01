namespace S3WebApi.GlobalLayer
{
    public class NetworkException : Exception
    {
        public NetworkException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
