using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SwishClient.UnitTests
{
    public class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _responseMessage;

        public TestHttpMessageHandler(HttpResponseMessage message)
        {
            _responseMessage = message;
        }

        public TestHttpMessageHandler(HttpStatusCode status)
            : this(CreateResponseMessage(status))
        { }

        public TestHttpMessageHandler(HttpStatusCode status, string content)
            : this(CreateJsonResponseMessage(status, content))
        { }

        public HttpRequestMessage Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(_responseMessage);
        }

        private static HttpResponseMessage CreateResponseMessage(HttpStatusCode status)
        {
            var emptyContent = new StringContent("", Encoding.UTF8, "application/json");
            return new HttpResponseMessage(status) { Content = emptyContent };
        }

        private static HttpResponseMessage CreateJsonResponseMessage(HttpStatusCode status, string content)
        {
            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");
            return new HttpResponseMessage(status) { Content = stringContent };
        }
    }
}