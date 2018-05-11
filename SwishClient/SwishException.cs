using System.Net.Http;

namespace SwishClient
{
    public class SwishException : HttpRequestException
    {
        public SwishException(string message) : base(message)
        { }
    }
}
